using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Services;

public interface IStockConsumptionService
{
    Task<ConsumeStockResponse> ConsumeAsync(
        ConsumeStockRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class StockConsumptionService : IStockConsumptionService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public StockConsumptionService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<ConsumeStockResponse> ConsumeAsync(
        ConsumeStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var ingredient = await _dbContext.Set<Ingredient>()
            .FirstOrDefaultAsync(
                i => i.Id == request.IngredientId
                     && (i.WorkspaceId == null || i.WorkspaceId == workspaceId),
                cancellationToken);

        if (ingredient is null)
        {
            throw new ServiceException(
                $"Ingredient '{request.IngredientId}' was not found for this workspace.",
                StatusCodes.Status404NotFound);
        }

        decimal quantityInBase;
        try
        {
            quantityInBase = UnitConversion.ToBaseUnits(request.Quantity, request.Unit, ingredient.BaseUnit);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            throw new ServiceException(ex.Message);
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var inventory = await _dbContext.Set<InventoryEntity>()
            .FirstOrDefaultAsync(i => i.IngredientId == ingredient.Id, cancellationToken);

        if (inventory is null || inventory.TotalQuantity < quantityInBase)
        {
            throw new ServiceException(
                $"Insufficient stock. Available: {inventory?.TotalQuantity ?? 0} {ingredient.BaseUnit}, requested: {quantityInBase}.",
                StatusCodes.Status409Conflict);
        }

        var batches = await _dbContext.Set<StockBatch>()
            .Where(b => b.IngredientId == ingredient.Id && b.CurrentQuantity > 0)
            .OrderBy(b => b.ReceivedAt)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var remaining = quantityInBase;
        var allocations = new List<ConsumeStockBatchAllocation>();
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
            var allocatedCost = Math.Round(unitCost * take, 4, MidpointRounding.AwayFromZero);

            batch.CurrentQuantity -= take;
            remaining -= take;
            totalCost += allocatedCost;

            allocations.Add(new ConsumeStockBatchAllocation(batch.Id, take, allocatedCost));
        }

        if (remaining > 0)
        {
            throw new ServiceException(
                "Insufficient stock across FIFO batches (inventory summary out of sync).",
                StatusCodes.Status409Conflict);
        }

        inventory.TotalQuantity -= quantityInBase;

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        await _dbContext.Set<InventoryMovement>().AddAsync(
            new InventoryMovement
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                IngredientId = ingredient.Id,
                Type = InventoryMovementType.Consume,
                Quantity = quantityInBase,
                SignedQuantity = -quantityInBase,
                TotalCost = totalCost,
                Source = "Manual consume",
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ConsumeStockResponse(
            ingredient.Id,
            ingredient.Name,
            quantityInBase,
            ingredient.BaseUnit.ToString(),
            totalCost,
            inventory.TotalQuantity,
            allocations);
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
