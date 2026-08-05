using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class UpdateWorkspaceManagerEndpoint
{
    public static RouteHandlerBuilder MapUpdateWorkspaceManagerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/manager", HandleAsync)
            .WithName("UpdateWorkspaceManager")
            .WithTags("Tenants");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateWorkspaceManagerRequest request,
        AppDbContext dbContext,
        IWorkspaceManagerProvisioner managerProvisioner,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and Password are required." });
        }

        var workspaceExists = await dbContext.Set<Workspace>()
            .AnyAsync(w => w.Id == id, cancellationToken);

        if (!workspaceExists)
        {
            return Results.NotFound(new { message = $"Workspace '{id}' was not found." });
        }

        try
        {
            await managerProvisioner.UpdatePrimaryCateringManagerAsync(
                id,
                request.Email,
                request.Password,
                cancellationToken);

            return Results.Ok(new { message = "CateringManager credentials updated.", workspaceId = id });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }
}
