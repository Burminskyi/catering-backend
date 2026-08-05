using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class GetWorkspacesEndpoint
{
    public static RouteHandlerBuilder MapGetWorkspacesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetWorkspaces")
            .WithTags("Tenants");
    }

    private static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var workspaces = await dbContext.Set<Workspace>()
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceResponse(
                w.Id,
                w.Name,
                w.Subdomain,
                w.CreatedAt,
                w.IsActive,
                w.SubscriptionExpiresAt,
                w.PlanType))
            .ToListAsync(cancellationToken);

        return Results.Ok(workspaces);
    }
}
