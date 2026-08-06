using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Services;

public interface IInventoryBalanceService
{
    Task<InventoryBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default);
}

public sealed class InventoryBalanceService : IInventoryBalanceService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public InventoryBalanceService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<InventoryBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var inventories = await _dbContext.Set<InventoryEntity>()
            .AsNoTracking()
            .Where(i => i.TotalQuantity > 0)
            .Include(i => i.Ingredient)
            .OrderBy(i => i.Ingredient.Name)
            .ToListAsync(cancellationToken);

        var ingredientIds = inventories.Select(i => i.IngredientId).ToList();

        var activeBatches = await _dbContext.Set<StockBatch>()
            .AsNoTracking()
            .Where(b => ingredientIds.Contains(b.IngredientId) && b.CurrentQuantity > 0)
            .ToListAsync(cancellationToken);

        var batchesByIngredient = activeBatches
            .GroupBy(b => b.IngredientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<InventoryBalanceItemResponse>();
        decimal grandTotal = 0m;

        foreach (var inv in inventories)
        {
            batchesByIngredient.TryGetValue(inv.IngredientId, out var batches);
            batches ??= [];

            decimal remainingValue = 0m;
            foreach (var batch in batches)
            {
                var unitCost = batch.InitialQuantity > 0
                    ? batch.CostPrice / batch.InitialQuantity
                    : 0m;
                remainingValue += unitCost * batch.CurrentQuantity;
            }

            remainingValue = Math.Round(remainingValue, 4, MidpointRounding.AwayFromZero);
            var avgUnitCost = inv.TotalQuantity > 0
                ? Math.Round(remainingValue / inv.TotalQuantity, 6, MidpointRounding.AwayFromZero)
                : 0m;

            grandTotal += remainingValue;

            items.Add(new InventoryBalanceItemResponse(
                inv.IngredientId,
                inv.Ingredient.Name,
                inv.Ingredient.Category.ToString(),
                inv.Ingredient.BaseUnit.ToString(),
                inv.TotalQuantity,
                avgUnitCost,
                remainingValue,
                batches.Count));
        }

        return new InventoryBalanceResponse(
            workspaceId,
            items,
            Math.Round(grandTotal, 4, MidpointRounding.AwayFromZero));
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new ServiceException("Workspace context is required.", StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }
}
