using CateringSaaS.Modules.Knowledge.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Knowledge.Endpoints;

public static class ListKnowledgeDocumentsEndpoint
{
    public static RouteHandlerBuilder MapListKnowledgeDocumentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/documents", HandleAsync)
            .WithName("ListKnowledgeDocuments")
            .WithTags("Knowledge");
    }

    private static async Task<IResult> HandleAsync(
        IKnowledgeBaseService knowledgeBase,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (tenantContext.WorkspaceId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Workspace context is required." });
        }

        var items = await knowledgeBase.ListDocumentsAsync(tenantContext.WorkspaceId, cancellationToken);
        return Results.Ok(items);
    }
}
