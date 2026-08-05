using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class UpdateWorkspaceStatusEndpoint
{
    public static RouteHandlerBuilder MapUpdateWorkspaceStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/status", HandleAsync)
            .WithName("UpdateWorkspaceStatus")
            .WithTags("Tenants");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateWorkspaceStatusRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Set<Workspace>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return Results.NotFound(new { message = $"Workspace '{id}' was not found." });
        }

        workspace.IsActive = request.IsActive;
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
