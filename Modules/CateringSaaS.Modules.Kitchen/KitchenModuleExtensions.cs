using CateringSaaS.Modules.Kitchen.Endpoints;
using CateringSaaS.Modules.Kitchen.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Kitchen;

public static class KitchenModuleExtensions
{
    public static IServiceCollection AddKitchenModule(this IServiceCollection services)
    {
        services.AddScoped<IProductionPlanService, ProductionPlanService>();
        return services;
    }

    public static IEndpointRouteBuilder MapKitchenEndpoints(this IEndpointRouteBuilder app)
    {
        var kitchen = app.MapGroup("/api/kitchen")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "Chef"));

        kitchen.MapGetProductionPlanEndpoint();
        kitchen.MapExecuteProductionPlanEndpoint();
        kitchen.MapGetShoppingListEndpoint();

        return app;
    }
}
