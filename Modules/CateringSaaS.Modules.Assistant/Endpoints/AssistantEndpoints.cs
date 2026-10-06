using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Assistant.Endpoints;

public static class AssistantEndpoints
{
    public static RouteHandlerBuilder MapChatEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/chat", HandleAsync)
            .WithName("AssistantChat")
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
            return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
        }
    }
}
