using CateringSaaS.Modules.Inventory.Domain.Enums;
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

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetAvailableQuantitiesAsync(
        Guid workspaceId,
        IEnumerable<Guid> ingredientIds,
        CancellationToken cancellationToken = default)
    {
        var ids = ingredientIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await _dbContext.Set<InventoryEntity>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId && ids.Contains(i.IngredientId))
            .ToDictionaryAsync(i => i.IngredientId, i => i.TotalQuantity, cancellationToken);
    }

    public async Task DeductStockFifoAsync(
        Guid workspaceId,
        IReadOnlyDictionary<Guid, decimal> ingredientsToDeduct,
        string? source = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var movementSource = string.IsNullOrWhiteSpace(source) ? "Kitchen production" : source.Trim();
        var movementReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

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
            decimal totalCost = 0m;

            foreach (var batch in batches)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var take = Math.Min(batch.CurrentQuantity, remaining);
                var unitCost = batch.InitialQuantity > 0
                    ? batch.CostPrice / batch.InitialQuantity
                    : 0m;
                totalCost += Math.Round(unitCost * take, 4, MidpointRounding.AwayFromZero);

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

            await _dbContext.Set<InventoryMovement>().AddAsync(
                new InventoryMovement
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    IngredientId = ingredientId,
                    Type = InventoryMovementType.Consume,
                    Quantity = quantityInBase,
                    SignedQuantity = -quantityInBase,
                    TotalCost = totalCost,
                    Source = movementSource,
                    Reason = movementReason,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
