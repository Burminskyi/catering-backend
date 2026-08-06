using CateringSaaS.Modules.Inventory.Data;
using CateringSaaS.Modules.Inventory.Endpoints;
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
        services.AddValidatorsFromAssemblyContaining<CreateIngredientValidator>(
            lifetime: ServiceLifetime.Scoped);

        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCreateIngredientEndpoint();
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
