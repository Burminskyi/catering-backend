using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Modules.Inventory.Data;

public sealed class InventoryDatabaseSeeder : IInventoryDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<InventoryDatabaseSeeder> _logger;

    public InventoryDatabaseSeeder(AppDbContext dbContext, ILogger<InventoryDatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Schema is applied via MigrateAsync in Identity DatabaseSeeder before this runs.
        var ingredients = _dbContext.Set<Ingredient>();

        var hasGlobalIngredients = await ingredients
            .AnyAsync(i => i.WorkspaceId == null, cancellationToken);

        if (hasGlobalIngredients)
        {
            return;
        }

        var seed = GlobalIngredientsSeedData.CreateAll();
        await ingredients.AddRangeAsync(seed, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} global ingredients (WorkspaceId = null).", seed.Count);
    }
}
