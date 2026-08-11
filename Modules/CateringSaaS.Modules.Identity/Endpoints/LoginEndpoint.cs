using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Modules.Identity.Services;
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

        User? user = null;

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim().ToLowerInvariant();
            user = await dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            user = await dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
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
            user.CompanyId));
    }
}

public sealed record LoginRequest(string? Username, string? Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    Guid UserId,
    string Username,
    string? Email,
    string Role,
    Guid? WorkspaceId,
    Guid? CompanyId);
