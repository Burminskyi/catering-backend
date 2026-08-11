using System.Security.Claims;
using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Modules.Identity.Services;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Endpoints;

public static class ImpersonateWorkspaceEndpoint
{
    public static RouteHandlerBuilder MapImpersonateWorkspaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/superadmin/impersonate/{workspaceId:guid}", HandleAsync)
            .WithName("ImpersonateWorkspace")
            .WithTags("SuperAdmin")
            .RequireAuthorization(policy => policy.RequireRole("SuperAdmin"));
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        IWorkspaceLookup workspaceLookup,
        JwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (!await workspaceLookup.ExistsAsync(workspaceId, cancellationToken))
        {
            return Results.NotFound(new { message = $"Workspace '{workspaceId}' was not found." });
        }

        var users = dbContext.Set<User>();

        var manager = await users
            .FirstOrDefaultAsync(
                u => u.WorkspaceId == workspaceId && u.Role == StaffRole.WorkspaceAdmin,
                cancellationToken);

        if (manager is null)
        {
            manager = new User
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Username = $"admin-{workspaceId:N}".ToLowerInvariant(),
                Email = null,
                PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N")),
                FirstName = "Impersonation",
                LastName = "Admin",
                Role = StaffRole.WorkspaceAdmin,
                IsActive = true,
                CompanyId = null
            };

            await users.AddAsync(manager, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var impersonatedBy = ResolveUserId(principal) ?? Guid.Empty;
        var token = tokenGenerator.GenerateImpersonationToken(manager, impersonatedBy);

        return Results.Ok(new ImpersonationResponse(
            token,
            workspaceId,
            manager.Id,
            StaffRole.WorkspaceAdmin.ToString()));
    }

    private static Guid? ResolveUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
