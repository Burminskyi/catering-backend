using System.ClientModel;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Configuration;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace CateringSaaS.Modules.Assistant.Services;

public interface IAssistantChatService
{
    Task<AssistantChatResponse> ChatAsync(
        AssistantChatRequest request,
        AssistantScope scope,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AssistantStreamEvent> StreamChatAsync(
        AssistantChatRequest request,
        AssistantScope scope,
        CancellationToken cancellationToken = default);
}

public sealed class AssistantChatService : IAssistantChatService
{
    private const int MaxToolTurns = 3;
    private const int MaxHistoryMessages = 10;

    /// <summary>Stable prefix for Groq prompt caching. Do not interpolate clocks or tenant data.</summary>
    private const string StaticSystemPrompt =
        """
        You are the operational AI assistant for a multi-tenant catering ERP (SmartCatering).
        A following system message states the client's local date, time, and timezone, plus server UTC.
        Do not calculate calendar dates yourself. Pass a period argument and let the server resolve it:
        preset (today, yesterday, this_week, last_week, next_week, this_month, last_month, next_month, this_year, last_year, next_year),
        lastDays (last 3 days → 3, includes today), lastHours (last 3 hours → 3), or explicit date / dateFrom+dateTo.
        Weeks start on Monday. this week/month/year ends today. last week/month/year is the full previous period.
        Order tools use a business date and cannot answer lastHours. Stock-movement tools can.
        A clock shift such as 12:00-15:00 is timeFrom/timeTo, separate from lastHours.
        Critical stock and current balances are a snapshot, not a history.
        State the period field returned by the tool. Do not name a different window.
        Language rules (mandatory):
        - Reply entirely in the language of the user's latest message (Ukrainian, Russian, Polish, or English).
        - A following system message states the detected reply language — obey it even if the UI locale differs.
        - Write dates, month names, and relative phrases in that same reply language. Do not mix languages in one answer.
        - Do not translate database field values (client names, dish names, ingredient names, supplier names, status codes as stored). Quote them as returned by tools.
        - Chart/table chrome (titles, column headers, legends) is localized by the server from the reply language; do not invent English labels in chat when the user wrote in another language.
        Knowledge / RAG rules (mandatory when search_knowledge_base returns documentChunks):
        - You are provided with context chunks from internal documents inside [DOCUMENT CHUNKS] / <context>.
        - The user query may be in Ukrainian, English, or Russian, while documents may be in another language.
        - Read and translate the facts from the provided context chunks to answer the user's question accurately in the user's language.
        - Rely ONLY on the provided context. Do not invent HACCP rules, temperatures, contract clauses, or recipes that are not in those chunks.
        - Quote concrete numbers, temperatures, and rule names from the chunks. Name the source document title.
        - If relevant is false or [DOCUMENT CHUNKS] says nothing was found, say so clearly and do not fabricate policy text.
        - For knowledge answers: use <<<CHAT>>> (short answer + source title) and <<<OVERVIEW>>> (detailed explanation from chunks). Do not invent chart/table markers.
        Tool names and parameter names stay in English. Do not invent data — call tools.
        Never ask for or accept a workspaceId; tenancy is enforced by the server from the JWT.
        Tool discipline (mandatory):
        - Call the minimum tools needed (usually one pulse/report tool).
        - Never call the same tool twice with the same arguments in one turn chain.
        - After tool results arrive, answer immediately — do not re-fetch the same data.
        - Obey each tool payload field answerFrom. State only numbers present in metrics, aggregates, highlights, or rows.
        - If a table has complete=false, do not paste a markdown table of rows and do not describe rows that are not in highlights. Say that the on-screen table lists all rowCount rows.
        - If complete=true, rows are exhaustive and may be cited.
        Response layout (mandatory when tools returned metrics/tables or you produce a multi-section report):
        Write these sections with markers on their own lines:
        <<<TITLE>>>
        One short workspace title in the reply language: topic + resolved period from the tool period field.
        Example (ru): Выручка по клиентам за период с 2026-09-09 по 2026-10-08
        Example (en): Client revenue for 2026-09-09 – 2026-10-08
        Rules: noun phrase, not a question, not a copy of the user message, no markdown, max ~90 characters.
        <<<CHAT>>>
        Start with a markdown H2 heading in the reply language (## Summary / ## Итог / ## Підсумок / ## Podsumowanie).
        Then 1–2 short sentences that name the resolved period from the tool period field (a single day or a from–to range) and frame the result (e.g. for this period we have…).
        After that, 2–6 bullets with the key numbers in bold. Do not dump numbers without saying which period they cover. No markdown tables. End by pointing to the results panel on the right (tables and charts) for details — never use the word artifacts in user-visible text (say results panel / панель с данными / panel wyników / панель результатів as appropriate).
        <<<OVERVIEW>>>
        Intent brief only — how you understood the request. 2–4 short sentences or bullets covering: what the user asked for, the resolved period (use the tool period field), and which data that implies (e.g. orders, revenue, kitchen readiness, critical stock). Do NOT repeat the numerical summary. Do NOT write a Summary/Итог heading here. Do NOT paste markdown tables here — charts and tables render separately below.
        If there is nothing to visualize (no tools / no report), omit the markers and write a normal short reply only.
        Use bold for key numbers. Avoid decorative ASCII separators.
        If a knowledge search returns no relevant documents, say so and do not fabricate policy or recipe text.
        """;

    /// <summary>Lean prompt for the post-RAG answer turn — saves TPM vs the full ERP system prompt.</summary>
    private const string KnowledgeAnswerSystemPrompt =
        """
        You are SmartCatering's knowledge assistant.
        You are provided with context chunks from internal documents inside [DOCUMENT CHUNKS] / <context>.
        The user query may be in Ukrainian, English, or Russian, while documents may be in another language.
        Read and translate the facts from the provided context chunks to answer the user's question accurately in the user's language.
        Rely ONLY on the provided context. Do not invent HACCP rules, temperatures, contract clauses, or recipes.
        Cite the source document title. If context says nothing relevant was found, say so clearly.
        Do not call tools. Use these markers on their own lines:
        <<<CHAT>>>
        Short answer (2–5 sentences) with the key rule/number and the source document title.
        <<<OVERVIEW>>>
        Detailed explanation from the chunks: all relevant rules, temperatures, timings, exceptions. Markdown bullets OK. No chart talk.
        """;

    private readonly GroqOptions _options;
    private readonly AssistantRagOptions _rag;
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;
    private readonly IAssistantConversationStore _conversations;
    private readonly IClientTimeContext _clock;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AssistantChatService> _logger;

    public AssistantChatService(
        IOptions<GroqOptions> options,
        IOptions<AssistantRagOptions> rag,
        IEnumerable<IAssistantTool> tools,
        IAssistantConversationStore conversations,
        IClientTimeContext clock,
        IHttpClientFactory httpClientFactory,
        ILogger<AssistantChatService> logger)
    {
        _options = options.Value;
        _rag = rag.Value;
        _tools = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        _conversations = conversations;
        _clock = clock;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AssistantChatResponse> ChatAsync(
        AssistantChatRequest request,
        AssistantScope scope,
        CancellationToken cancellationToken = default)
    {
        var replyLanguage = AssistantLanguage.Detect(request.Message);
        using var _ = new CultureScope(replyLanguage);

        var (key, conversationId, userMessage) = PrepareConversation(request, scope);
        var stored = await LoadConversationAsync(key, userMessage, cancellationToken);
        var client = CreateChatClient();
        var collectedArtifacts = new List<AssistantArtifact>();
        var toolCache = new Dictionary<string, ToolResult>(StringComparer.Ordinal);
        var finalText = string.Empty;
        var allowTools = true;

        for (var turn = 0; turn < MaxToolTurns; turn++)
        {
            var window = BuildModelWindow(stored, replyLanguage, forceAnswer: !allowTools);
            ChatCompletion completion = await CompleteChatAsync(
                client,
                window,
                BuildChatOptions(allowTools),
                cancellationToken);
            stored.Add(new AssistantChatMessage(completion));

            if (allowTools
                && completion.FinishReason == ChatFinishReason.ToolCalls
                && completion.ToolCalls.Count > 0)
            {
                var cacheHits = 0;
                foreach (var toolCall in completion.ToolCalls)
                {
                    var (toolResult, fromCache) = await ExecuteToolCallAsync(toolCall, scope, toolCache, cancellationToken);
                    if (fromCache)
                    {
                        cacheHits++;
                    }
                    else if (toolResult.Artifacts is { Count: > 0 })
                    {
                        collectedArtifacts.AddRange(toolResult.Artifacts);
                    }

                    stored.Add(new ToolChatMessage(toolCall.Id, ReportArtifactMapper.ToToolJson(toolResult)));
                }

                // Knowledge answers need no further tools; drop schemas on the next turn to save TPM.
                if (cacheHits == completion.ToolCalls.Count
                    || completion.ToolCalls.All(c =>
                        string.Equals(c.FunctionName, "search_knowledge_base", StringComparison.Ordinal)))
                {
                    allowTools = false;
                }

                continue;
            }

            finalText = completion.Content.Count > 0
                ? string.Concat(completion.Content.Select(part => part.Text))
                : string.Empty;
            break;
        }

        if (string.IsNullOrWhiteSpace(finalText) && collectedArtifacts.Count > 0)
        {
            finalText = FallbackResultsText(replyLanguage);
        }

        var (chatText, artifactsOut, title) = FinalizeAssistantReply(finalText, collectedArtifacts, stored);
        await _conversations.SaveAsync(key, stored, artifactsOut, userMessage, title, cancellationToken);

        return new AssistantChatResponse(conversationId, chatText, artifactsOut, title);
    }

    public async IAsyncEnumerable<AssistantStreamEvent> StreamChatAsync(
        AssistantChatRequest request,
        AssistantScope scope,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var replyLanguage = AssistantLanguage.Detect(request.Message);
        using var cultureScope = new CultureScope(replyLanguage);

        var (key, conversationId, userMessage) = PrepareConversation(request, scope);
        yield return new AssistantStatusEvent("thinking");

        var stored = await LoadConversationAsync(key, userMessage, cancellationToken);
        var client = CreateChatClient();
        var collectedArtifacts = new List<AssistantArtifact>();
        var toolCache = new Dictionary<string, ToolResult>(StringComparer.Ordinal);
        var finalText = string.Empty;
        var allowTools = true;
        var ranTools = false;

        for (var turn = 0; turn < MaxToolTurns; turn++)
        {
            yield return new AssistantStatusEvent(ranTools || !allowTools ? "analyzing" : "thinking");

            var window = BuildModelWindow(stored, replyLanguage, forceAnswer: !allowTools);
            var chatOptions = BuildChatOptions(allowTools);
            var content = new StringBuilder();
            var toolCalls = new StreamingToolCallAccumulator();
            var sawToolCalls = false;
            ChatFinishReason? finish = null;

            await foreach (var update in client.CompleteChatStreamingAsync(window, chatOptions, cancellationToken))
            {
                if (update.ToolCallUpdates.Count > 0)
                {
                    sawToolCalls = true;
                    foreach (var toolUpdate in update.ToolCallUpdates)
                    {
                        toolCalls.Append(toolUpdate);
                    }
                }

                foreach (var part in update.ContentUpdate)
                {
                    if (string.IsNullOrEmpty(part.Text))
                    {
                        continue;
                    }

                    // Buffer the final answer; emit chat tokens only after CHAT/OVERVIEW split.
                    content.Append(part.Text);
                }

                if (update.FinishReason is { } reason)
                {
                    finish = reason;
                }
            }

            var builtCalls = toolCalls.Build();
            if (allowTools && (finish == ChatFinishReason.ToolCalls || sawToolCalls) && builtCalls.Count > 0)
            {
                var assistantMessage = new AssistantChatMessage(builtCalls);
                if (content.Length > 0)
                {
                    assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(content.ToString()));
                }

                stored.Add(assistantMessage);

                yield return new AssistantStatusEvent("querying");

                var cacheHits = 0;
                foreach (var toolCall in builtCalls)
                {
                    var (toolResult, fromCache) = await ExecuteToolCallAsync(toolCall, scope, toolCache, cancellationToken);
                    if (fromCache)
                    {
                        cacheHits++;
                    }
                    else if (toolResult.Artifacts is { Count: > 0 })
                    {
                        collectedArtifacts.AddRange(toolResult.Artifacts);
                    }

                    stored.Add(new ToolChatMessage(toolCall.Id, ReportArtifactMapper.ToToolJson(toolResult)));
                }

                ranTools = true;
                // Knowledge answers need no further tools; drop schemas on the next turn to save TPM.
                if (cacheHits == builtCalls.Count
                    || builtCalls.All(c =>
                        string.Equals(c.FunctionName, "search_knowledge_base", StringComparison.Ordinal)))
                {
                    allowTools = false;
                }

                continue;
            }

            finalText = content.ToString();
            stored.Add(new AssistantChatMessage(finalText));
            break;
        }

        yield return new AssistantStatusEvent("finalizing");

        if (string.IsNullOrWhiteSpace(finalText) && collectedArtifacts.Count > 0)
        {
            finalText = FallbackResultsText(replyLanguage);
        }

        var (chatText, artifactsOut, title) = FinalizeAssistantReply(finalText, collectedArtifacts, stored);
        await _conversations.SaveAsync(key, stored, artifactsOut, userMessage, title, cancellationToken);

        if (chatText.Length > 0)
        {
            yield return new AssistantTokenEvent(chatText);
        }

        yield return new AssistantArtifactsEvent(artifactsOut);
        yield return new AssistantDoneEvent(conversationId, title);
    }

    /// <summary>
    /// Keep the persisted assistant message short for chat; attach detailed markdown as OverviewArtifact.
    /// </summary>
    private static (string ChatText, List<AssistantArtifact> Artifacts, string? Title) FinalizeAssistantReply(
        string finalText,
        List<AssistantArtifact> collectedArtifacts,
        List<ChatMessage> stored)
    {
        var hasKnowledgeSources = collectedArtifacts.Any(a => a is KnowledgeSourcesArtifact);
        var preferOverview = hasKnowledgeSources
            || collectedArtifacts.Count > 0
            || finalText.Contains("<<<OVERVIEW>>>", StringComparison.OrdinalIgnoreCase)
            || finalText.Contains("##OVERVIEW##", StringComparison.OrdinalIgnoreCase)
            || finalText.Contains('|');

        var (chatText, overview, title) = AssistantReplySplitter.Split(finalText, preferOverview);
        if (string.IsNullOrWhiteSpace(chatText))
        {
            chatText = finalText.Trim();
        }

        // Replace the last assistant text turn with the short chat version (history stays concise).
        for (var i = stored.Count - 1; i >= 0; i--)
        {
            if (stored[i] is AssistantChatMessage assistant
                && assistant.ToolCalls.Count == 0
                && assistant.Content.Count > 0)
            {
                stored[i] = new AssistantChatMessage(chatText);
                break;
            }
        }

        var artifactsOut = new List<AssistantArtifact>(collectedArtifacts.Count + 1);
        artifactsOut.AddRange(collectedArtifacts.Where(a => a is not OverviewArtifact));

        // Avoid duplicating the same short answer in chat and overview for knowledge replies.
        if (!string.IsNullOrWhiteSpace(overview)
            && !(hasKnowledgeSources && IsNearDuplicate(chatText, overview)))
        {
            artifactsOut.Insert(0, new OverviewArtifact(overview.Trim()));
        }

        return (chatText, artifactsOut, title);
    }

    private static bool IsNearDuplicate(string left, string right)
    {
        var a = NormalizeForCompare(left);
        var b = NormalizeForCompare(right);
        if (a.Length == 0 || b.Length == 0)
        {
            return false;
        }

        if (a == b)
        {
            return true;
        }

        var shorter = a.Length <= b.Length ? a : b;
        var longer = a.Length <= b.Length ? b : a;
        return longer.StartsWith(shorter, StringComparison.Ordinal) && longer.Length <= shorter.Length + 40;
    }

    private static string NormalizeForCompare(string value) =>
        string.Join(
            ' ',
            value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private (ConversationKey Key, Guid ConversationId, string UserMessage) PrepareConversation(
        AssistantChatRequest request,
        AssistantScope scope)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Groq API key is not configured.");
            throw Unreachable();
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new AssistantServiceException("Message is required.", StatusCodes.Status400BadRequest);
        }

        if (scope.WorkspaceId == Guid.Empty || scope.UserId == Guid.Empty)
        {
            throw new AssistantServiceException(
                "Authentication with workspace context is required.",
                StatusCodes.Status401Unauthorized);
        }

        var conversationId = request.ConversationId is Guid existing && existing != Guid.Empty
            ? existing
            : Guid.NewGuid();

        var key = new ConversationKey(scope.WorkspaceId, scope.UserId, conversationId);
        return (key, conversationId, request.Message.Trim());
    }

    private async Task<List<ChatMessage>> LoadConversationAsync(
        ConversationKey key,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var stored = (await _conversations.GetOrCreateAsync(key, cancellationToken))
            .Where(message => message is not SystemChatMessage)
            .ToList();
        stored.Add(new UserChatMessage(userMessage));
        return stored;
    }

    private async Task<ChatCompletion> CompleteChatAsync(
        ChatClient client,
        IEnumerable<ChatMessage> window,
        ChatCompletionOptions chatOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.CompleteChatAsync(window, chatOptions, cancellationToken);
        }
        catch (Exception ex) when (AssistantProviderFailures.IsRateLimited(ex))
        {
            throw RateLimited(ex);
        }
        catch (Exception ex) when (AssistantProviderFailures.IsUnreachable(ex))
        {
            throw Unreachable(ex);
        }
    }

    private AssistantServiceException Unreachable(Exception? exception = null)
    {
        if (exception is not null)
        {
            _logger.LogError(exception, "Groq API is unreachable after retries.");
        }

        return new AssistantServiceException(
            "The assistant is temporarily unavailable. Please try again in a moment.",
            StatusCodes.Status503ServiceUnavailable,
            AssistantServiceException.UnreachableCode);
    }

    private AssistantServiceException RateLimited(Exception? exception = null)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception, "Groq API rate limit exceeded after retries.");
        }

        return new AssistantServiceException(
            "The assistant is busy right now (rate limit). Please wait about 20 seconds and try again.",
            StatusCodes.Status429TooManyRequests,
            AssistantServiceException.RateLimitedCode);
    }

    private List<ChatMessage> BuildModelWindow(IReadOnlyList<ChatMessage> stored, string replyLanguage, bool forceAnswer = false)
    {
        var conversational = stored.Where(message => message is not SystemChatMessage).ToList();
        if (conversational.Count > MaxHistoryMessages)
        {
            var start = conversational.Count - MaxHistoryMessages;
            while (start < conversational.Count && conversational[start] is ToolChatMessage)
            {
                start++;
            }

            conversational = conversational[start..];
        }

        // Free-tier TPM: lean window. Production (UseLeanKnowledgeWindow=false) keeps full ERP prompt + chunks.
        if (forceAnswer
            && _rag.UseLeanKnowledgeWindow
            && TryBuildKnowledgeAnswerWindow(conversational, replyLanguage, out var knowledgeWindow))
        {
            return knowledgeWindow;
        }

        var window = new List<ChatMessage>(conversational.Count + 3)
        {
            new SystemChatMessage(StaticSystemPrompt),
            new SystemChatMessage(BuildClockPrompt()),
            new SystemChatMessage(BuildLanguagePrompt(replyLanguage))
        };
        if (forceAnswer)
        {
            window.Add(new SystemChatMessage(
                """
                Tool results for this request are already complete. Do not call tools.
                If the latest tool payload contains [DOCUMENT CHUNKS] / <context>: answer ONLY from that context, translate facts into the user's language, cite document titles, and write a normal short reply (no <<<TITLE>>>/<<<CHAT>>>/<<<OVERVIEW>>> markers).
                Otherwise (metrics/tables): Write <<<TITLE>>> (short topic + period, not a question), <<<CHAT>>> (markdown ## Summary/Итог heading, 1–2 sentences naming the tool period, then key-number bullets) and <<<OVERVIEW>>> (intent brief: understood request, period, implied metrics — no Summary heading, no number dump) now. Use only those facts.
                """));
        }

        window.AddRange(conversational);
        return window;
    }

    private static bool TryBuildKnowledgeAnswerWindow(
        IReadOnlyList<ChatMessage> conversational,
        string replyLanguage,
        out List<ChatMessage> window)
    {
        window = [];
        string? documentChunks = null;
        for (var i = conversational.Count - 1; i >= 0; i--)
        {
            if (conversational[i] is not ToolChatMessage toolMessage)
            {
                continue;
            }

            var raw = ExtractToolMessageText(toolMessage);
            if (raw is null || !raw.Contains("[DOCUMENT CHUNKS]", StringComparison.Ordinal))
            {
                continue;
            }

            documentChunks = ExtractDocumentChunks(raw) ?? raw;
            break;
        }

        if (documentChunks is null)
        {
            return false;
        }

        UserChatMessage? lastUser = null;
        for (var i = conversational.Count - 1; i >= 0; i--)
        {
            if (conversational[i] is UserChatMessage user)
            {
                lastUser = user;
                break;
            }
        }

        if (lastUser is null)
        {
            return false;
        }

        window =
        [
            new SystemChatMessage(KnowledgeAnswerSystemPrompt),
            new SystemChatMessage(BuildLanguagePrompt(replyLanguage)),
            lastUser,
            new SystemChatMessage(documentChunks)
        ];
        return true;
    }

    private static string? ExtractToolMessageText(ToolChatMessage message)
    {
        if (message.Content.Count == 0)
        {
            return null;
        }

        return string.Concat(message.Content.Select(part => part.Text));
    }

    private static string? ExtractDocumentChunks(string toolJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(toolJson);
            if (!doc.RootElement.TryGetProperty("documentChunks", out var node))
            {
                return null;
            }

            var text = node.GetString();
            return !string.IsNullOrWhiteSpace(text)
                && text.Contains("[DOCUMENT CHUNKS]", StringComparison.Ordinal)
                ? text
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private string BuildClockPrompt()
    {
        var local = _clock.LocalNow;
        var utc = DateTime.UtcNow;
        return
            $"""
            Client Local Time: {local:yyyy-MM-dd HH:mm} {_clock.TimeZoneId}
            Server UTC: {utc:yyyy-MM-dd HH:mm:ss} UTC
            Local calendar date for today, yesterday, and week boundaries: {local:yyyy-MM-dd}.
            """;
    }

    private static string BuildLanguagePrompt(string replyLanguage)
    {
        var name = AssistantLanguage.DisplayName(replyLanguage);
        return
            $"""
            Detected reply language: {name} (code={replyLanguage}).
            Write the entire assistant message in {name} only.
            Format human-readable dates and month names in {name}.
            Keep tool/database entity names (clients, dishes, ingredients, suppliers) exactly as provided by tools.
            """;
    }

    private static string FallbackResultsText(string replyLanguage) => replyLanguage switch
    {
        "uk" => "Ось результати.",
        "pl" => "Oto wyniki.",
        "ru" => "Вот результаты.",
        _ => "Here are the results."
    };

    private ChatClient CreateChatClient()
    {
        var httpClient = _httpClientFactory.CreateClient(GroqOptions.HttpClientName);
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = ResolveGroqEndpoint(_options.BaseUrl),
            Transport = new HttpClientPipelineTransport(httpClient),
            NetworkTimeout = TimeSpan.FromMinutes(2),
            // Polly on the Groq HttpClient owns 429/5xx retries.
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
        };

        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? "openai/gpt-oss-20b"
            : _options.Model.Trim();

        return new ChatClient(
            model: model,
            credential: new ApiKeyCredential(_options.ApiKey.Trim()),
            options: clientOptions);
    }

    private static Uri ResolveGroqEndpoint(string? baseUrl)
    {
        const string fallback = "https://api.groq.com/openai/v1";
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new Uri(fallback);
        }

        var trimmed = baseUrl.Trim().Trim('"', '\'');
        if (trimmed.StartsWith('[') && trimmed.Contains("](", StringComparison.Ordinal))
        {
            var close = trimmed.IndexOf(']');
            if (close > 1)
            {
                trimmed = trimmed[1..close];
            }
        }

        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed.TrimStart('/');
        }

        if (Uri.TryCreate(trimmed.TrimEnd('/'), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri;
        }

        return new Uri(fallback);
    }

    private ChatCompletionOptions BuildChatOptions(bool allowTools = true)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = 0
        };

        if (!allowTools)
        {
            return options;
        }

        foreach (var tool in _tools.Values)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: BinaryData.FromString(tool.ParametersSchema.ToJsonString())));
        }

        return options;
    }

    private async Task<(ToolResult Result, bool FromCache)> ExecuteToolCallAsync(
        ChatToolCall toolCall,
        AssistantScope scope,
        Dictionary<string, ToolResult> cache,
        CancellationToken cancellationToken)
    {
        var cacheKey = toolCall.FunctionName + "\n" + toolCall.FunctionArguments.ToString().Trim();
        if (cache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogInformation("Reused in-request tool cache for {Tool}", toolCall.FunctionName);
            return (cached, true);
        }

        var result = await ExecuteToolCallCoreAsync(toolCall, scope, cancellationToken);
        cache[cacheKey] = result;
        return (result, false);
    }

    private async Task<ToolResult> ExecuteToolCallCoreAsync(
        ChatToolCall toolCall,
        AssistantScope scope,
        CancellationToken cancellationToken)
    {
        if (!_tools.TryGetValue(toolCall.FunctionName, out var tool))
        {
            _logger.LogWarning("Assistant requested unknown tool {Tool}", toolCall.FunctionName);
            return new ToolResult(new { error = $"Unknown tool '{toolCall.FunctionName}'." });
        }

        JsonObject args;
        try
        {
            var raw = toolCall.FunctionArguments.ToString();
            args = string.IsNullOrWhiteSpace(raw)
                ? new JsonObject()
                : JsonNode.Parse(raw) as JsonObject ?? new JsonObject();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool arguments for {Tool}", tool.Name);
            return new ToolResult(new { error = "Invalid tool arguments JSON." });
        }

        // Strip any attempted workspace override from the model.
        args.Remove("workspaceId");
        args.Remove("WorkspaceId");

        try
        {
            return await tool.ExecuteAsync(args, scope, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {Tool} failed for workspace {WorkspaceId}", tool.Name, scope.WorkspaceId);
            return new ToolResult(new { error = ex.Message });
        }
    }
}

public sealed class AssistantServiceException : Exception
{
    public const string UnreachableCode = "assistant_unreachable";
    public const string RateLimitedCode = "assistant_rate_limited";

    public int StatusCode { get; }

    public string? Code { get; }

    public AssistantServiceException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        string? code = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

internal static class AssistantProviderFailures
{
    public static bool IsRateLimited(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is ClientResultException clientEx)
            {
                if (clientEx.Status == 429)
                {
                    return true;
                }

                if (clientEx.Message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase)
                    || clientEx.Message.Contains("tokens per minute", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (current.Message.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsUnreachable(Exception exception)
    {
        if (IsRateLimited(exception))
        {
            return false;
        }

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException)
            {
                return false;
            }

            if (current is HttpRequestException)
            {
                return true;
            }

            if (current is ClientResultException)
            {
                return true;
            }
        }

        return false;
    }
}
