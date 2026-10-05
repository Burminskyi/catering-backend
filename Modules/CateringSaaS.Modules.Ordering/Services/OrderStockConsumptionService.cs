using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

/// <summary>
/// FIFO stock consume when an order enters production (InProduction).
/// Idempotent via <see cref="Order.StockConsumedAt"/>.
/// </summary>
public interface IOrderStockConsumptionService
{
    Task ConsumeForOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task RestoreForOrderAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class OrderStockConsumptionService : IOrderStockConsumptionService
{
    private readonly AppDbContext _dbContext;
    private readonly IDishRecipeCatalog _recipeCatalog;
    private readonly IInventoryManager _inventoryManager;

    public OrderStockConsumptionService(
        AppDbContext dbContext,
        IDishRecipeCatalog recipeCatalog,
        IInventoryManager inventoryManager)
    {
        _dbContext = dbContext;
        _recipeCatalog = recipeCatalog;
        _inventoryManager = inventoryManager;
    }

    public async Task ConsumeForOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.StockConsumedAt is not null)
        {
            return;
        }

        var ownsTransaction = _dbContext.Database.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await ConsumeLockedAsync(order, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await ConsumeLockedAsync(order, cancellationToken);
    }

    public async Task RestoreForOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var ownsTransaction = _dbContext.Database.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await RestoreLockedAsync(order, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await RestoreLockedAsync(order, cancellationToken);
    }

    private async Task ConsumeLockedAsync(Order order, CancellationToken cancellationToken)
    {
        await LockOrderRowAsync(order.Id, cancellationToken);

        var consumedAt = await _dbContext.Set<Order>()
            .Where(o => o.Id == order.Id)
            .Select(o => o.StockConsumedAt)
            .FirstAsync(cancellationToken);

        if (consumedAt is not null)
        {
            order.StockConsumedAt = consumedAt;
            return;
        }

        var ingredientTotals = await BuildIngredientTotalsAsync(order, cancellationToken);

        var availability = await _inventoryManager.CheckStockAvailabilityAsync(
            order.WorkspaceId,
            ingredientTotals,
            cancellationToken);

        if (!availability.IsAvailable)
        {
            var details = string.Join(
                "; ",
                availability.Shortages.Select(s =>
                    $"{s.IngredientName}: need {s.RequiredQuantity} {s.Unit}, available {s.AvailableQuantity} {s.Unit}"));

            throw new OrderServiceException(
                $"Insufficient stock: {details}",
                StatusCodes.Status409Conflict);
        }

        try
        {
            await _inventoryManager.DeductStockFifoAsync(
                order.WorkspaceId,
                ingredientTotals,
                source: $"Order {order.Id}",
                reason: "OrderProduction",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OrderServiceException and not OperationCanceledException)
        {
            // Inventory throws module-local ServiceException; map without a project reference.
            throw new OrderServiceException(ex.Message, StatusCodes.Status409Conflict);
        }

        order.StockConsumedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RestoreLockedAsync(Order order, CancellationToken cancellationToken)
    {
        await LockOrderRowAsync(order.Id, cancellationToken);

        var consumedAt = await _dbContext.Set<Order>()
            .Where(o => o.Id == order.Id)
            .Select(o => o.StockConsumedAt)
            .FirstAsync(cancellationToken);

        if (consumedAt is null)
        {
            order.StockConsumedAt = null;
            return;
        }

        var ingredientTotals = await BuildIngredientTotalsAsync(order, cancellationToken);

        try
        {
            await _inventoryManager.RestoreStockAsync(
                order.WorkspaceId,
                ingredientTotals,
                source: $"Order {order.Id}",
                reason: "OrderCancelled",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OrderServiceException and not OperationCanceledException)
        {
            throw new OrderServiceException(ex.Message, StatusCodes.Status409Conflict);
        }

        order.StockConsumedAt = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal>> BuildIngredientTotalsAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        if (order.Items is null || order.Items.Count == 0 || order.Items.Sum(i => i.Quantity) <= 0)
        {
            throw new OrderServiceException(
                "Cannot change stock: order has no dish portions.",
                StatusCodes.Status409Conflict);
        }

        var recipes = await _recipeCatalog.GetRecipesByMenuItemIdsAsync(
            order.WorkspaceId,
            order.Items.Select(i => i.MenuItemId),
            cancellationToken);

        var ingredientTotals = new Dictionary<Guid, decimal>();

        foreach (var item in order.Items)
        {
            if (!recipes.TryGetValue(item.MenuItemId, out var recipe))
            {
                var label = string.IsNullOrWhiteSpace(item.DishName)
                    ? item.MenuItemId.ToString()
                    : item.DishName;
                throw new OrderServiceException(
                    $"Cannot change stock: recipe not found for dish '{label}'.",
                    StatusCodes.Status409Conflict);
            }

            if (recipe.Ingredients.Count == 0)
            {
                throw new OrderServiceException(
                    $"Cannot change stock: dish '{recipe.DishName}' has no tech-card ingredients.",
                    StatusCodes.Status409Conflict);
            }

            if (item.DishId is null)
            {
                item.DishId = recipe.DishId;
            }

            if (string.IsNullOrWhiteSpace(item.DishName))
            {
                item.DishName = recipe.DishName;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                var need = ingredient.QuantityPerPortion * item.Quantity;
                ingredientTotals.TryGetValue(ingredient.IngredientId, out var current);
                ingredientTotals[ingredient.IngredientId] = current + need;
            }
        }

        return ingredientTotals;
    }

    private Task LockOrderRowAsync(Guid orderId, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Order stock changes require an open transaction.");
        }

        return _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM orders WHERE "Id" = {orderId} FOR UPDATE""",
            cancellationToken);
    }
}

internal static class OrderDtoMapper
{
    public static OrderItemResponse ToItemResponse(OrderItem item) =>
        new(
            item.Id,
            item.MenuItemId,
            item.DishId,
            item.DishName,
            item.Quantity,
            item.UnitPrice,
            item.Subtotal);

    public static OrderResponse ToResponse(Order order)
    {
        var items = order.Items.Select(ToItemResponse).ToList();
        return new OrderResponse(
            order.Id,
            order.WorkspaceId,
            order.ClientCompanyId,
            order.PlacedByUserId,
            order.DriverId,
            order.TargetDate,
            order.CreatedAt,
            order.Status.ToString(),
            order.TotalAmount,
            items.Count,
            items.Sum(i => i.Quantity),
            items);
    }

    public static OrderListItemResponse ToListItem(Order order, ClientCompanyContact? client = null)
    {
        var items = order.Items.Select(ToItemResponse).ToList();
        return new OrderListItemResponse(
            order.Id,
            order.ClientCompanyId,
            order.PlacedByUserId,
            order.DriverId,
            order.TargetDate,
            order.CreatedAt,
            order.Status.ToString(),
            order.TotalAmount,
            items.Count,
            items.Sum(i => i.Quantity),
            items,
            client?.Name,
            client?.Address,
            client?.ContactName,
            client?.ContactPhone);
    }
}
