using CateringSaaS.Modules.Identity.Domain;
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        // MVP: plain-text password comparison (PasswordHash stores the raw password for seed users)
        if (user is null || !string.Equals(user.PasswordHash, request.Password, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        var token = tokenGenerator.Generate(user);

        return Results.Ok(new LoginResponse(
            token,
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.WorkspaceId,
            user.CompanyId));
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    Guid UserId,
    string Email,
    string Role,
    Guid? WorkspaceId,
    Guid? CompanyId);
