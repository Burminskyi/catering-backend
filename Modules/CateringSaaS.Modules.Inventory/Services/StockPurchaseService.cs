using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Services;

public interface IStockPurchaseService
{
    Task<StockPurchaseResponse> AddPurchaseAsync(
        AddStockPurchaseRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class StockPurchaseService : IStockPurchaseService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public StockPurchaseService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<StockPurchaseResponse> AddPurchaseAsync(
        AddStockPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        if (request.SupplierId == Guid.Empty)
        {
            throw new ServiceException("SupplierId is required.");
        }

        if (request.TotalCost < 0)
        {
            throw new ServiceException("TotalCost cannot be negative.");
        }

        var ingredient = await ResolveAccessibleIngredientAsync(request.IngredientId, workspaceId, cancellationToken);

        var supplier = await _dbContext.Set<Supplier>()
            .FirstOrDefaultAsync(
                s => s.Id == request.SupplierId && s.WorkspaceId == workspaceId && s.IsActive,
                cancellationToken);

        if (supplier is null)
        {
            throw new ServiceException(
                $"Active supplier '{request.SupplierId}' was not found.",
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

        var receivedAt = request.PurchasedAt.HasValue
            ? DateTime.SpecifyKind(request.PurchasedAt.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var batch = new StockBatch
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            IngredientId = ingredient.Id,
            SupplierId = supplier.Id,
            InitialQuantity = quantityInBase,
            CurrentQuantity = quantityInBase,
            CostPrice = request.TotalCost,
            ReceivedAt = receivedAt
        };

        await _dbContext.Set<StockBatch>().AddAsync(batch, cancellationToken);

        var inventory = await _dbContext.Set<InventoryEntity>()
            .FirstOrDefaultAsync(i => i.IngredientId == ingredient.Id, cancellationToken);

        if (inventory is null)
        {
            inventory = new InventoryEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                IngredientId = ingredient.Id,
                TotalQuantity = quantityInBase
            };
            await _dbContext.Set<InventoryEntity>().AddAsync(inventory, cancellationToken);
        }
        else
        {
            inventory.TotalQuantity += quantityInBase;
        }

        await _dbContext.Set<InventoryMovement>().AddAsync(
            new InventoryMovement
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                IngredientId = ingredient.Id,
                Type = InventoryMovementType.Purchase,
                Quantity = quantityInBase,
                SignedQuantity = quantityInBase,
                TotalCost = request.TotalCost,
                Source = supplier.Name,
                Reason = null,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var unitCost = quantityInBase > 0 ? request.TotalCost / quantityInBase : 0m;

        return new StockPurchaseResponse(
            batch.Id,
            ingredient.Id,
            ingredient.Name,
            supplier.Id,
            supplier.Name,
            quantityInBase,
            ingredient.BaseUnit.ToString(),
            request.TotalCost,
            unitCost,
            receivedAt,
            inventory.TotalQuantity);
    }

    private async Task<Ingredient> ResolveAccessibleIngredientAsync(
        Guid ingredientId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
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

        return ingredient;
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
