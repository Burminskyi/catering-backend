using CateringSaaS.Modules.Knowledge.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Knowledge.Endpoints;

public static class DeleteKnowledgeDocumentEndpoint
{
    public static RouteHandlerBuilder MapDeleteKnowledgeDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/documents/{id:guid}", HandleAsync)
            .WithName("DeleteKnowledgeDocument")
            .WithTags("Knowledge");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IKnowledgeBaseService knowledgeBase,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (tenantContext.WorkspaceId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Workspace context is required." });
        }

        var deleted = await knowledgeBase.DeleteDocumentAsync(id, tenantContext.WorkspaceId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound(new { message = "Document not found." });
    }
}
