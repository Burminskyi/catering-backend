using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using CateringSaaS.Shared.Notifications;
using CateringSaaS.Shared.SeedData;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace CateringSaaS.Shared;

public static class SharedServiceExtensions
{
    public static IServiceCollection AddSharedPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ModuleConfigurationRegistry.Register(typeof(WorkspaceNotificationConfiguration).Assembly);

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<IClientTimeContext, HttpClientTimeContext>();
        services.AddScoped<IOperationalDemoSeeder, NullOperationalDemoSeeder>();
        services.AddScoped<WorkspaceNotificationService>();
        services.AddScoped<IWorkspaceNotificationPublisher>(sp => sp.GetRequiredService<WorkspaceNotificationService>());
        services.AddScoped<IWorkspaceNotificationQueries>(sp => sp.GetRequiredService<WorkspaceNotificationService>());

        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsql.UseVector();
                });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapSharedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapNotificationEndpoints();
        return app;
    }
}
