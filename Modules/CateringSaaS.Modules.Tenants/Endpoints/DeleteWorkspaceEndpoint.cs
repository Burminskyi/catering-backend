using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class DeleteWorkspaceEndpoint
{
    public static RouteHandlerBuilder MapDeleteWorkspaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteWorkspace")
            .WithTags("Tenants");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Set<Workspace>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return Results.NotFound(new { message = $"Workspace '{id}' was not found." });
        }

        // Soft-delete: deactivate instead of removing the row
        workspace.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new WorkspaceResponse(
            workspace.Id,
            workspace.Name,
            workspace.Subdomain,
            workspace.CreatedAt,
            workspace.IsActive,
            workspace.SubscriptionExpiresAt,
            workspace.PlanType));
    }
}
