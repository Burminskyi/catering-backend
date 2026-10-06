using System.ClientModel;
using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Configuration;
using CateringSaaS.Modules.Assistant.Contracts;
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
}

public sealed class AssistantChatService : IAssistantChatService
{
    private const int MaxToolTurns = 4;

    private readonly GroqOptions _options;
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;
    private readonly IAssistantConversationStore _conversations;
    private readonly ILogger<AssistantChatService> _logger;

    public AssistantChatService(
        IOptions<GroqOptions> options,
        IEnumerable<IAssistantTool> tools,
        IAssistantConversationStore conversations,
        ILogger<AssistantChatService> logger)
    {
        _options = options.Value;
        _tools = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        _conversations = conversations;
        _logger = logger;
    }

    public async Task<AssistantChatResponse> ChatAsync(
        AssistantChatRequest request,
        AssistantScope scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AssistantServiceException(
                "Groq API key is not configured.",
                StatusCodes.Status503ServiceUnavailable);
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

        var history = _conversations.GetOrCreate(conversationId).ToList();
        if (history.Count == 0 || history[0] is not SystemChatMessage)
        {
            history.Insert(0, new SystemChatMessage(BuildSystemPrompt()));
        }
        else
        {
            history[0] = new SystemChatMessage(BuildSystemPrompt());
        }

        history.Add(new UserChatMessage(request.Message.Trim()));

        var client = CreateChatClient();
        var chatOptions = BuildChatOptions();
        var collectedArtifacts = new List<AssistantArtifact>();
        var finalText = string.Empty;

        for (var turn = 0; turn < MaxToolTurns; turn++)
        {
            ChatCompletion completion = await client.CompleteChatAsync(history, chatOptions, cancellationToken);
            history.Add(new AssistantChatMessage(completion));

            if (completion.FinishReason == ChatFinishReason.ToolCalls && completion.ToolCalls.Count > 0)
            {
                foreach (var toolCall in completion.ToolCalls)
                {
                    var toolResult = await ExecuteToolCallAsync(toolCall, scope, cancellationToken);
                    if (toolResult.Artifacts is { Count: > 0 })
                    {
                        collectedArtifacts.AddRange(toolResult.Artifacts);
                    }

                    history.Add(new ToolChatMessage(toolCall.Id, ReportArtifactMapper.ToToolJson(toolResult)));
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

        _conversations.Save(conversationId, history);

        return new AssistantChatResponse(conversationId, finalText, collectedArtifacts);
    }

    private ChatClient CreateChatClient()
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(_options.BaseUrl)
        };

        return new ChatClient(
            model: _options.Model,
            credential: new ApiKeyCredential(_options.ApiKey),
            options: clientOptions);
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

    private static string BuildSystemPrompt()
    {
        var now = DateTime.UtcNow;
        return
            $"""
            You are the operational AI assistant for a multi-tenant catering ERP (mise.).
            Current server UTC date/time: {now:yyyy-MM-dd HH:mm:ss} UTC (ISO date today: {now:yyyy-MM-dd}).
            When the user mentions relative dates (yesterday, last Friday, прошлую пятницу, wczoraj, this week, last 10 days),
            convert them to absolute ISO dates (yyyy-MM-dd) using the current UTC date above before calling tools.

            Always reply in the same language as the user's latest message (Ukrainian, English, Polish, or Russian).
            Tool names and parameter names stay in English. Do not invent data — call tools.
            Never ask for or accept a workspaceId; tenancy is enforced by the server from the JWT.
            Prefer concise answers and highlight key metrics from tool results.
            """;
    }
}

public sealed class AssistantServiceException : Exception
{
    public int StatusCode { get; }

    public AssistantServiceException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
