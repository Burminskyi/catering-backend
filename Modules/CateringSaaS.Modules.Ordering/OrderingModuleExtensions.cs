using CateringSaaS.Modules.Ordering.Data;
using CateringSaaS.Modules.Ordering.Endpoints;
using CateringSaaS.Modules.Ordering.Services;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Ordering;

public static class OrderingModuleExtensions
{
    public static IServiceCollection AddOrderingModule(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.Register(typeof(OrderConfiguration).Assembly);

        services.AddScoped<IClientOrderService, ClientOrderService>();
        services.AddScoped<IWorkspaceOrderService, WorkspaceOrderService>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        var clientPortal = app.MapGroup("/api/client-portal")
            .RequireAuthorization(policy => policy.RequireRole("ClientAdmin", "ClientEmployee"));

        clientPortal.MapCreateClientOrderEndpoint();
        clientPortal.MapGetClientOrdersEndpoint();
        clientPortal.MapCancelClientOrderEndpoint();

        var orders = app.MapGroup("/api/orders")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin", "Manager"));

        orders.MapGetWorkspaceOrdersEndpoint();
        orders.MapUpdateOrderStatusEndpoint();

        return app;
    }
}
