using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Assistant.Endpoints;

public static class DeleteAssistantConversationEndpoint
{
    public static RouteHandlerBuilder MapDeleteAssistantConversationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/conversations/{conversationId:guid}", HandleAsync)
            .WithName("DeleteAssistantConversation")
            .WithTags("Assistant");
    }

    private static async Task<IResult> HandleAsync(
        Guid conversationId,
        IAssistantConversationHistory history,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var deleted = await history.DeleteAsync(
            tenantContext.WorkspaceId,
            currentUser.UserId,
            conversationId,
            cancellationToken);

        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
