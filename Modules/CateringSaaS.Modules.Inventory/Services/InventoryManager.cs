using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Services;

public sealed class InventoryManager : IInventoryManager
{
    private readonly AppDbContext _dbContext;
    private readonly IIngredientCatalog _ingredientCatalog;

    public InventoryManager(AppDbContext dbContext, IIngredientCatalog ingredientCatalog)
    {
        _dbContext = dbContext;
        _ingredientCatalog = ingredientCatalog;
    }

    public async Task<StockAvailabilityResult> CheckStockAvailabilityAsync(
        Guid workspaceId,
        IReadOnlyDictionary<Guid, decimal> requiredIngredients,
        CancellationToken cancellationToken = default)
    {
        if (requiredIngredients.Count == 0)
        {
            return new StockAvailabilityResult(true, []);
        }

        var ingredientIds = requiredIngredients.Keys.ToArray();

        var inventories = await _dbContext.Set<InventoryEntity>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId && ingredientIds.Contains(i.IngredientId))
            .ToDictionaryAsync(i => i.IngredientId, i => i.TotalQuantity, cancellationToken);

        var catalog = await _ingredientCatalog.GetByIdsAsync(ingredientIds, workspaceId, cancellationToken);

        var shortages = new List<StockShortage>();

        foreach (var (ingredientId, required) in requiredIngredients)
        {
            inventories.TryGetValue(ingredientId, out var available);
            if (available >= required)
            {
                continue;
            }

            catalog.TryGetValue(ingredientId, out var info);
            shortages.Add(new StockShortage(
                ingredientId,
                info?.Name ?? "Unknown",
                required,
                available,
                info?.BaseUnit ?? "Unknown"));
        }

        return new StockAvailabilityResult(shortages.Count == 0, shortages);
    }

    public async Task DeductStockFifoAsync(
        Guid workspaceId,
        IReadOnlyDictionary<Guid, decimal> ingredientsToDeduct,
        CancellationToken cancellationToken = default)
    {
        foreach (var (ingredientId, quantityInBase) in ingredientsToDeduct)
        {
            if (quantityInBase <= 0)
            {
                continue;
            }

            var ingredient = await _dbContext.Set<Ingredient>()
                .FirstOrDefaultAsync(
                    i => i.Id == ingredientId
                         && (i.WorkspaceId == null || i.WorkspaceId == workspaceId),
                    cancellationToken);

            if (ingredient is null)
            {
                throw new ServiceException(
                    $"Ingredient '{ingredientId}' was not found for this workspace.",
                    StatusCodes.Status404NotFound);
            }

            var inventory = await _dbContext.Set<InventoryEntity>()
                .FirstOrDefaultAsync(
                    i => i.WorkspaceId == workspaceId && i.IngredientId == ingredientId,
                    cancellationToken);

            if (inventory is null || inventory.TotalQuantity < quantityInBase)
            {
                throw new ServiceException(
                    $"Insufficient stock for ingredient '{ingredient.Name}'.",
                    StatusCodes.Status409Conflict);
            }

            var batches = await _dbContext.Set<StockBatch>()
                .Where(b =>
                    b.WorkspaceId == workspaceId
                    && b.IngredientId == ingredientId
                    && b.CurrentQuantity > 0)
                .OrderBy(b => b.ReceivedAt)
                .ThenBy(b => b.Id)
                .ToListAsync(cancellationToken);

            var remaining = quantityInBase;

            foreach (var batch in batches)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var take = Math.Min(batch.CurrentQuantity, remaining);
                batch.CurrentQuantity -= take;
                remaining -= take;
            }

            if (remaining > 0)
            {
                throw new ServiceException(
                    "Insufficient stock across FIFO batches (inventory summary out of sync).",
                    StatusCodes.Status409Conflict);
            }

            inventory.TotalQuantity -= quantityInBase;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
