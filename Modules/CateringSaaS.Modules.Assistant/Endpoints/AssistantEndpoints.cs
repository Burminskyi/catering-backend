using System.Text.Json;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Modules.Assistant.Endpoints;

public static class AssistantEndpoints
{
    public static RouteHandlerBuilder MapChatEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/chat", HandleAsync)
            .WithName("AssistantChat")
            .WithTags("Assistant");
    }

    public static RouteHandlerBuilder MapChatStreamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/chat/stream", HandleStreamAsync)
            .WithName("AssistantChatStream")
            .WithTags("Assistant");
    }

    private static async Task<IResult> HandleAsync(
        AssistantChatRequest request,
        IAssistantChatService chatService,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = new AssistantScope(tenantContext.WorkspaceId, currentUser.UserId);
            var response = await chatService.ChatAsync(request, scope, cancellationToken);
            return Results.Ok(response);
        }
        catch (AssistantServiceException ex)
        {
            return Results.Json(new { message = ex.Message, code = ex.Code }, statusCode: ex.StatusCode);
        }
    }

    private static async Task HandleStreamAsync(
        AssistantChatRequest request,
        HttpContext httpContext,
        IAssistantChatService chatService,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AssistantChatStream");
        var response = httpContext.Response;
        var started = false;

        try
        {
            var scope = new AssistantScope(tenantContext.WorkspaceId, currentUser.UserId);
            await foreach (var streamEvent in chatService.StreamChatAsync(request, scope, cancellationToken))
            {
                if (!started)
                {
                    StartSse(httpContext);
                    started = true;
                }

                switch (streamEvent)
                {
                    case AssistantStatusEvent status:
                        await AssistantSseWriter.WriteAsync(
                            response,
                            "status",
                            JsonSerializer.Serialize(new { stage = status.Stage }, AssistantSseWriter.JsonOptions),
                            cancellationToken);
                        break;
                    case AssistantTokenEvent token:
                        await AssistantSseWriter.WriteAsync(
                            response,
                            "token",
                            JsonSerializer.Serialize(token.Text, AssistantSseWriter.JsonOptions),
                            cancellationToken);
                        break;
                    case AssistantArtifactsEvent artifacts:
                        await AssistantSseWriter.WriteAsync(
                            response,
                            "artifacts",
                            JsonSerializer.Serialize(artifacts.Artifacts, AssistantSseWriter.JsonOptions),
                            cancellationToken);
                        break;
                    case AssistantDoneEvent done:
                        await AssistantSseWriter.WriteAsync(
                            response,
                            "done",
                            JsonSerializer.Serialize(
                                new { conversationId = done.ConversationId, title = done.Title },
                                AssistantSseWriter.JsonOptions),
                            cancellationToken);
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser closed the stream.
        }
        catch (AssistantServiceException ex)
        {
            await WriteFailureAsync(response, started, ex.StatusCode, ex.Message, ex.Code, cancellationToken);
        }
        catch (Exception ex) when (AssistantProviderFailures.IsRateLimited(ex))
        {
            logger.LogWarning(ex, "Groq API rate limit exceeded after retries.");
            await WriteFailureAsync(
                response,
                started,
                StatusCodes.Status429TooManyRequests,
                "The assistant is busy right now (rate limit). Please wait about 20 seconds and try again.",
                AssistantServiceException.RateLimitedCode,
                cancellationToken);
        }
        catch (Exception ex) when (AssistantProviderFailures.IsUnreachable(ex))
        {
            logger.LogError(ex, "Groq API is unreachable after retries.");
            await WriteFailureAsync(
                response,
                started,
                StatusCodes.Status503ServiceUnavailable,
                "The assistant is temporarily unavailable. Please try again in a moment.",
                AssistantServiceException.UnreachableCode,
                cancellationToken);
        }
    }

    private static void StartSse(HttpContext httpContext)
    {
        var response = httpContext.Response;
        httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache, no-transform";
        response.Headers.Append("X-Accel-Buffering", "no");
    }

    private static async Task WriteFailureAsync(
        HttpResponse response,
        bool started,
        int statusCode,
        string message,
        string? code,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!started)
        {
            response.StatusCode = statusCode;
            await response.WriteAsJsonAsync(new { message, code }, cancellationToken);
            return;
        }

        var payload = JsonSerializer.Serialize(new { message, code }, AssistantSseWriter.JsonOptions);
        await AssistantSseWriter.WriteAsync(response, "error", payload, cancellationToken);
    }
}
