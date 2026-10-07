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
    private const int MaxToolTurns = 4;
    private const int MaxHistoryMessages = 12;

    /// <summary>Stable prefix for Groq prompt caching. Do not interpolate clocks or tenant data.</summary>
    private const string StaticSystemPrompt =
        """
        You are the operational AI assistant for a multi-tenant catering ERP (mise.).
        A following system message states the client's local date, time, and timezone, plus server UTC.
        When the user mentions relative dates (yesterday, last Friday, прошлую пятницу, wczoraj, this week, last 10 days),
        convert them to absolute ISO dates (yyyy-MM-dd) using the client local calendar date, not UTC.
        dateFrom and dateTo are local calendar dates.
        For stock, food-cost, consumption, and supplier tools, a clock window such as 12:00-15:00 must be passed as timeFrom and timeTo (HH:mm, 24-hour, client local).
        Order, delivery, revenue, and pulse tools store a business date (TargetDate) only. For those, use the local calendar day and do not invent hourly totals.
        Always reply in the same language as the user's latest message (Ukrainian, English, Polish, or Russian).
        Tool names and parameter names stay in English. Do not invent data — call tools.
        Never ask for or accept a workspaceId; tenancy is enforced by the server from the JWT.
        Prefer concise answers and highlight key metrics from tool results.
        If a knowledge search returns no relevant documents, say so and do not fabricate policy or recipe text.
        """;

    private readonly GroqOptions _options;
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;
    private readonly IAssistantConversationStore _conversations;
    private readonly IClientTimeContext _clock;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AssistantChatService> _logger;

    public AssistantChatService(
        IOptions<GroqOptions> options,
        IEnumerable<IAssistantTool> tools,
        IAssistantConversationStore conversations,
        IClientTimeContext clock,
        IHttpClientFactory httpClientFactory,
        ILogger<AssistantChatService> logger)
    {
        _options = options.Value;
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
        var (key, conversationId, stored) = PrepareConversation(request, scope);
        var client = CreateChatClient();
        var chatOptions = BuildChatOptions();
        var collectedArtifacts = new List<AssistantArtifact>();
        var finalText = string.Empty;

        for (var turn = 0; turn < MaxToolTurns; turn++)
        {
            var window = BuildModelWindow(stored);
            ChatCompletion completion = await CompleteChatAsync(client, window, chatOptions, cancellationToken);
            stored.Add(new AssistantChatMessage(completion));

            if (completion.FinishReason == ChatFinishReason.ToolCalls && completion.ToolCalls.Count > 0)
            {
                foreach (var toolCall in completion.ToolCalls)
                {
                    var toolResult = await ExecuteToolCallAsync(toolCall, scope, cancellationToken);
                    if (toolResult.Artifacts is { Count: > 0 })
                    {
                        collectedArtifacts.AddRange(toolResult.Artifacts);
                    }

                    stored.Add(new ToolChatMessage(toolCall.Id, ReportArtifactMapper.ToToolJson(toolResult)));
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
            finalText = "Here are the results.";
        }

        _conversations.Save(key, stored);

        return new AssistantChatResponse(conversationId, finalText, collectedArtifacts);
    }

    public async IAsyncEnumerable<AssistantStreamEvent> StreamChatAsync(
        AssistantChatRequest request,
        AssistantScope scope,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (key, conversationId, stored) = PrepareConversation(request, scope);
        var client = CreateChatClient();
        var chatOptions = BuildChatOptions();
        var collectedArtifacts = new List<AssistantArtifact>();
        var finalText = string.Empty;

        for (var turn = 0; turn < MaxToolTurns; turn++)
        {
            var window = BuildModelWindow(stored);
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

                    content.Append(part.Text);

                    // Tool turns stay silent. Tokens are emitted only once this turn has no tool calls.
                    if (!sawToolCalls)
                    {
                        yield return new AssistantTokenEvent(part.Text);
                    }
                }

                if (update.FinishReason is { } reason)
                {
                    finish = reason;
                }
            }

            var builtCalls = toolCalls.Build();
            if ((finish == ChatFinishReason.ToolCalls || sawToolCalls) && builtCalls.Count > 0)
            {
                var assistantMessage = new AssistantChatMessage(builtCalls);
                if (content.Length > 0)
                {
                    assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(content.ToString()));
                }

                stored.Add(assistantMessage);

                foreach (var toolCall in builtCalls)
                {
                    var toolResult = await ExecuteToolCallAsync(toolCall, scope, cancellationToken);
                    if (toolResult.Artifacts is { Count: > 0 })
                    {
                        collectedArtifacts.AddRange(toolResult.Artifacts);
                    }

                    stored.Add(new ToolChatMessage(toolCall.Id, ReportArtifactMapper.ToToolJson(toolResult)));
                }

                continue;
            }

            finalText = content.ToString();
            stored.Add(new AssistantChatMessage(finalText));
            break;
        }

        if (string.IsNullOrWhiteSpace(finalText) && collectedArtifacts.Count > 0)
        {
            finalText = "Here are the results.";
        }

        _conversations.Save(key, stored);

        yield return new AssistantArtifactsEvent(collectedArtifacts);
        yield return new AssistantDoneEvent(conversationId);
    }

    private (ConversationKey Key, Guid ConversationId, List<ChatMessage> Stored) PrepareConversation(
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
        var stored = _conversations.GetOrCreate(key)
            .Where(message => message is not SystemChatMessage)
            .ToList();
        stored.Add(new UserChatMessage(request.Message.Trim()));
        return (key, conversationId, stored);
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

    private List<ChatMessage> BuildModelWindow(IReadOnlyList<ChatMessage> stored)
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

        var window = new List<ChatMessage>(conversational.Count + 2)
        {
            new SystemChatMessage(StaticSystemPrompt),
            new SystemChatMessage(BuildClockPrompt())
        };
        window.AddRange(conversational);
        return window;
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
            ? "llama-3.1-8b-instant"
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

    private ChatCompletionOptions BuildChatOptions()
    {
        var options = new ChatCompletionOptions();

        foreach (var tool in _tools.Values)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: BinaryData.FromString(tool.ParametersSchema.ToJsonString())));
        }

        return options;
    }

    private async Task<ToolResult> ExecuteToolCallAsync(
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
    public static bool IsUnreachable(Exception exception)
    {
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
