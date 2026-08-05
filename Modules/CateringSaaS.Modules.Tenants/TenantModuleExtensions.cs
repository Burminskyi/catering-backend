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
        return services;
    }

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces")
            .RequireAuthorization(policy => policy.RequireRole("SuperAdmin"));

        group.MapCreateWorkspaceEndpoint();
        group.MapGetWorkspacesEndpoint();
        group.MapUpdateWorkspaceStatusEndpoint();
        group.MapDeleteWorkspaceEndpoint();
        group.MapUpdateWorkspaceSubscriptionEndpoint();
        group.MapUpdateWorkspaceManagerEndpoint();

        return app;
    }
}
