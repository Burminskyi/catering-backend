using CateringSaaS.Modules.Tenants.Data;
using CateringSaaS.Modules.Tenants.Endpoints;
using CateringSaaS.Modules.Tenants.Services;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Tenants;

public static class TenantModuleExtensions
{
    public static IServiceCollection AddTenantModule(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.Register(typeof(WorkspaceConfiguration).Assembly);
        services.AddScoped<IWorkspaceLookup, WorkspaceLookup>();
        services.AddScoped<IClientCompanyService, ClientCompanyService>();
        services.AddScoped<ITenantDataSeeder, TenantDatabaseSeeder>();
        return services;
    }

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaces = app.MapGroup("/api/workspaces")
            .RequireAuthorization(policy => policy.RequireRole("SuperAdmin"));

        workspaces.MapCreateWorkspaceEndpoint();
        workspaces.MapGetWorkspacesEndpoint();
        workspaces.MapUpdateWorkspaceStatusEndpoint();
        workspaces.MapDeleteWorkspaceEndpoint();
        workspaces.MapUpdateWorkspaceSubscriptionEndpoint();
        workspaces.MapUpdateWorkspaceManagerEndpoint();

        var clients = app.MapGroup("/api/clients")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin"));

        clients.MapGetClientsEndpoint();
        clients.MapCreateClientEndpoint();

        return app;
    }
}
