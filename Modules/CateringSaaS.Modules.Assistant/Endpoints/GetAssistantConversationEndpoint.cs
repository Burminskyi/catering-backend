using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Assistant.Endpoints;

public static class GetAssistantConversationEndpoint
{
    public static RouteHandlerBuilder MapGetAssistantConversationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/conversations/{conversationId:guid}", HandleAsync)
            .WithName("GetAssistantConversation")
            .WithTags("Assistant");
    }

    private static async Task<IResult> HandleAsync(
        Guid conversationId,
        IAssistantConversationHistory history,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var detail = await history.GetAsync(
            tenantContext.WorkspaceId,
            currentUser.UserId,
            conversationId,
            cancellationToken);

        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }
}
