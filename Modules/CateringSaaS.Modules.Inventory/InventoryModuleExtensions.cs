using CateringSaaS.Modules.Inventory.Data;
using CateringSaaS.Modules.Inventory.Endpoints;
using CateringSaaS.Modules.Inventory.Services;
using CateringSaaS.Modules.Inventory.Validators;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Inventory;

public static class InventoryModuleExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.Register(typeof(IngredientConfiguration).Assembly);

        services.AddScoped<IInventoryDataSeeder, InventoryDatabaseSeeder>();
        services.AddScoped<IIngredientCatalog, IngredientCatalog>();
        services.AddScoped<IInventoryManager, InventoryManager>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<IStockPurchaseService, StockPurchaseService>();
        services.AddScoped<IStockConsumptionService, StockConsumptionService>();
        services.AddScoped<IInventoryBalanceService, InventoryBalanceService>();

        services.AddValidatorsFromAssemblyContaining<CreateIngredientValidator>(
            lifetime: ServiceLifetime.Scoped);

        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var ingredients = app.MapGroup("/api/ingredients")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "Chef",
                "Driver",
                "Staff",
                "SuperAdmin"));

        ingredients.MapGetIngredientsEndpoint();
        ingredients.MapCreateIngredientEndpoint();
        ingredients.MapUpdateIngredientEndpoint();
        ingredients.MapDeleteIngredientEndpoint();

        var inventory = app.MapGroup("/api/inventory")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "Chef",
                "Driver",
                "Staff",
                "SuperAdmin"));

        inventory.MapAddStockPurchaseEndpoint();
        inventory.MapConsumeStockEndpoint();
        inventory.MapGetInventoryBalanceEndpoint();

        return app;
    }

    public static async Task UseInventoryModuleAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IInventoryDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
