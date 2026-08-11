using System.Text;
using CateringSaaS.Modules.Identity.Data;
using CateringSaaS.Modules.Identity.Endpoints;
using CateringSaaS.Modules.Identity.Services;
using CateringSaaS.Modules.Identity.Validators;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CateringSaaS.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ModuleConfigurationRegistry.Register(typeof(UserConfiguration).Assembly);

        services.AddSingleton<JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IWorkspaceManagerProvisioner, WorkspaceManagerProvisioner>();
        services.AddScoped<IClientAdminProvisioner, ClientAdminProvisioner>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IProfileService, ProfileService>();

        services.AddValidatorsFromAssemblyContaining<CreateStaffMemberValidator>(
            lifetime: ServiceLifetime.Scoped);

        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = configuration["Jwt:Audience"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = true,
                    RoleClaimType = "role",
                    NameClaimType = "sub"
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapLoginEndpoint();
        endpoints.MapImpersonateWorkspaceEndpoint();

        var staff = endpoints.MapGroup("/api/staff")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin"));

        staff.MapGetStaffEndpoint();
        staff.MapCreateStaffEndpoint();
        staff.MapUpdateStaffEndpoint();
        staff.MapDeleteStaffEndpoint();

        var profile = endpoints.MapGroup("/api/profile")
            .RequireAuthorization();

        profile.MapGetProfileEndpoint();
        profile.MapUpdateProfileEndpoint();
        profile.MapChangePasswordEndpoint();

        return endpoints;
    }

    public static async Task UseIdentityModuleAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
