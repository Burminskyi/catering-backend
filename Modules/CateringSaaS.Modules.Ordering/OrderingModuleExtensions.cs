using CateringSaaS.Modules.Ordering.Data;
using CateringSaaS.Modules.Ordering.Endpoints;
using CateringSaaS.Modules.Ordering.Services;
using CateringSaaS.Shared.Contracts;
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
        services.AddScoped<IProductionOrderGateway, ProductionOrderGateway>();
        services.AddScoped<IEmployeeMealRequestService, EmployeeMealRequestService>();
        services.AddScoped<IClientAdminMealRequestService, ClientAdminMealRequestService>();
        services.AddScoped<IMealReviewService, MealReviewService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IPushNotificationService, LoggingPushNotificationService>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        var clientPortal = app.MapGroup("/api/client-portal")
            .RequireAuthorization(policy => policy.RequireRole("ClientAdmin", "ClientEmployee"));

        clientPortal.MapCreateClientOrderEndpoint();
        clientPortal.MapGetClientOrdersEndpoint();
        clientPortal.MapCancelClientOrderEndpoint();

        var employeePortal = app.MapGroup("/api/client-portal")
            .RequireAuthorization(policy => policy.RequireRole("ClientEmployee"));

        employeePortal.MapCreateMealRequestEndpoint();
        employeePortal.MapGetMyMealRequestsEndpoint();
        employeePortal.MapCreateMealReviewEndpoint();

        var clientAdminPortal = app.MapGroup("/api/client-portal")
            .RequireAuthorization(policy => policy.RequireRole("ClientAdmin"));

        clientAdminPortal.MapGetSubmittedMealRequestsEndpoint();
        clientAdminPortal.MapConsolidateMealRequestsEndpoint();

        var orders = app.MapGroup("/api/orders")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin", "Manager"));

        orders.MapGetWorkspaceOrdersEndpoint();
        orders.MapUpdateOrderStatusEndpoint();
        orders.MapMarkOrderReadyEndpoint();
        orders.MapAssignDriverEndpoint();

        var reviews = app.MapGroup("/api/reviews")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin", "Manager"));

        reviews.MapGetWorkspaceReviewsEndpoint();

        var delivery = app.MapGroup("/api/delivery")
            .RequireAuthorization(policy => policy.RequireRole("Driver"));

        delivery.MapGetMyDeliveryOrdersEndpoint();
        delivery.MapDeliverOrderEndpoint();

        return app;
    }
}
