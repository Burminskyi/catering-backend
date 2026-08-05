using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class CreateWorkspaceEndpoint
{
    public static RouteHandlerBuilder MapCreateWorkspaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleAsync)
            .WithName("CreateWorkspace")
            .WithTags("Tenants");
    }

    private static async Task<IResult> HandleAsync(
        CreateWorkspaceRequest request,
        AppDbContext dbContext,
        IWorkspaceManagerProvisioner managerProvisioner,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Subdomain))
        {
            return Results.BadRequest(new { message = "Name and Subdomain are required." });
        }

        if (string.IsNullOrWhiteSpace(request.PlanType))
        {
            return Results.BadRequest(new { message = "PlanType is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ManagerEmail) || string.IsNullOrWhiteSpace(request.ManagerPassword))
        {
            return Results.BadRequest(new { message = "ManagerEmail and ManagerPassword are required." });
        }

        var subdomain = request.Subdomain.Trim().ToLowerInvariant();
        var planType = request.PlanType.Trim();

        var exists = await dbContext.Set<Workspace>()
            .AnyAsync(w => w.Subdomain == subdomain, cancellationToken);

        if (exists)
        {
            return Results.Conflict(new { message = $"Subdomain '{subdomain}' is already taken." });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Subdomain = subdomain,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                SubscriptionExpiresAt = DateTime.SpecifyKind(request.SubscriptionExpiresAt, DateTimeKind.Utc),
                PlanType = planType
            };

            await dbContext.Set<Workspace>().AddAsync(workspace, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await managerProvisioner.ProvisionCateringManagerAsync(
                workspace.Id,
                request.ManagerEmail,
                request.ManagerPassword,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Results.Created($"/api/workspaces/{workspace.Id}", ToResponse(workspace));
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = ex.Message });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static WorkspaceResponse ToResponse(Workspace workspace) =>
        new(
            workspace.Id,
            workspace.Name,
            workspace.Subdomain,
            workspace.CreatedAt,
            workspace.IsActive,
            workspace.SubscriptionExpiresAt,
            workspace.PlanType);
}
