using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class UpdateWorkspaceSubscriptionEndpoint
{
    public static RouteHandlerBuilder MapUpdateWorkspaceSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/subscription", HandleAsync)
            .WithName("UpdateWorkspaceSubscription")
            .WithTags("Tenants");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateWorkspaceSubscriptionRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PlanType))
        {
            return Results.BadRequest(new { message = "PlanType is required." });
        }

        var workspace = await dbContext.Set<Workspace>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return Results.NotFound(new { message = $"Workspace '{id}' was not found." });
        }

        workspace.SubscriptionExpiresAt = DateTime.SpecifyKind(request.SubscriptionExpiresAt, DateTimeKind.Utc);
        workspace.PlanType = request.PlanType.Trim();

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
