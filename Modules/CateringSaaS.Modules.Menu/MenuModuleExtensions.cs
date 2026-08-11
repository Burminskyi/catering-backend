using CateringSaaS.Modules.Menu.Data;
using CateringSaaS.Modules.Menu.Endpoints;
using CateringSaaS.Modules.Menu.Services;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Menu;

public static class MenuModuleExtensions
{
    public static IServiceCollection AddMenuModule(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.Register(typeof(DishConfiguration).Assembly);

        services.AddScoped<IDishService, DishService>();
        services.AddScoped<IMenuService, MenuService>();

        return services;
    }

    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var dishes = app.MapGroup("/api/dishes")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin"));

        dishes.MapGetDishesEndpoint();
        dishes.MapCreateDishEndpoint();
        dishes.MapUpdateDishEndpoint();
        dishes.MapDeleteDishEndpoint();

        var menus = app.MapGroup("/api/menus")
            .RequireAuthorization(policy => policy.RequireRole("WorkspaceAdmin"));

        menus.MapGetMenusEndpoint();
        menus.MapCreateMenuEndpoint();
        menus.MapAddMenuItemEndpoint();
        menus.MapUpdateMenuStatusEndpoint();

        var clientPortal = app.MapGroup("/api/client-portal")
            .RequireAuthorization(policy => policy.RequireRole("ClientAdmin", "ClientEmployee"));

        clientPortal.MapGetActiveClientMenusEndpoint();

        return app;
    }
}
