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

public static class LoginEndpoint
{
    public static RouteHandlerBuilder MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/auth/login", HandleAsync)
            .WithName("Login")
            .WithTags("Identity");
    }

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        AppDbContext dbContext,
        IWorkspaceLookup workspaceLookup,
        JwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Password is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Username) && string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { message = "Username or Email is required." });
        }

        Guid? resolvedWorkspaceId = null;

        if (!string.IsNullOrWhiteSpace(request.Subdomain))
        {
            resolvedWorkspaceId = await workspaceLookup.ResolveWorkspaceIdBySubdomainAsync(
                request.Subdomain,
                cancellationToken);

            if (resolvedWorkspaceId is null)
            {
                return Results.NotFound(new { message = $"Workspace with subdomain '{request.Subdomain.Trim().ToLowerInvariant()}' was not found." });
            }
        }

        User? user = null;

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim().ToLowerInvariant();
            user = await FindUserByUsernameAsync(
                dbContext,
                username,
                resolvedWorkspaceId,
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            user = await FindUserByEmailAsync(
                dbContext,
                email,
                resolvedWorkspaceId,
                cancellationToken);
        }

        if (user is null
            || !user.IsActive
            || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = tokenGenerator.Generate(user);

        return Results.Ok(new LoginResponse(
            token,
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString(),
            user.WorkspaceId,
            user.ClientCompanyId,
            user.CompanyId));
    }

    private static Task<User?> FindUserByUsernameAsync(
        AppDbContext dbContext,
        string username,
        Guid? workspaceId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<User>().Where(u => u.Username == username);

        query = workspaceId is Guid wsId
            ? query.Where(u => u.WorkspaceId == wsId)
            : query.Where(u => u.WorkspaceId == null);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    private static Task<User?> FindUserByEmailAsync(
        AppDbContext dbContext,
        string email,
        Guid? workspaceId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<User>().Where(u => u.Email == email);

        query = workspaceId is Guid wsId
            ? query.Where(u => u.WorkspaceId == wsId)
            : query.Where(u => u.WorkspaceId == null);

        return query.FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed record LoginRequest(string? Username, string? Email, string Password, string? Subdomain);

public sealed record LoginResponse(
    string AccessToken,
    Guid UserId,
    string Username,
    string? Email,
    string Role,
    Guid? WorkspaceId,
    Guid? ClientCompanyId,
    Guid? CompanyId);
