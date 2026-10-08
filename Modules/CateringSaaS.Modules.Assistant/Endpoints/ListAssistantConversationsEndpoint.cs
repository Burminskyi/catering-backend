using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Assistant.Endpoints;

public static class ListAssistantConversationsEndpoint
{
    public static RouteHandlerBuilder MapListAssistantConversationsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/conversations", HandleAsync)
            .WithName("ListAssistantConversations")
            .WithTags("Assistant");
    }

    private static async Task<IResult> HandleAsync(
        IAssistantConversationHistory history,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var items = await history.ListAsync(tenantContext.WorkspaceId, currentUser.UserId, cancellationToken);
        return Results.Ok(items);
    }
}
