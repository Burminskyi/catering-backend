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

        if (order.Items is null || order.Items.Count == 0 || order.Items.Sum(i => i.Quantity) <= 0)
        {
            throw new OrderServiceException(
                "Cannot start production: order has no dish portions to produce.",
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
                    $"Cannot consume stock: recipe not found for dish '{label}'.",
                    StatusCodes.Status409Conflict);
            }

            if (recipe.Ingredients.Count == 0)
            {
                throw new OrderServiceException(
                    $"Cannot consume stock: dish '{recipe.DishName}' has no tech-card ingredients.",
                    StatusCodes.Status409Conflict);
            }

            // Backfill snapshot if missing (legacy rows).
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

    public static OrderListItemResponse ToListItem(Order order)
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
            items);
    }
}
