using CateringSaaS.Modules.Kitchen.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Kitchen.Services;

public interface IProductionPlanService
{
    Task<ProductionPlanResponse> GetPlanAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    Task<ExecuteProductionPlanResponse> ExecutePlanAsync(
        ExecuteProductionPlanRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanService : IProductionPlanService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IProductionOrderGateway _orderGateway;
    private readonly IDishRecipeCatalog _recipeCatalog;
    private readonly IIngredientCatalog _ingredientCatalog;
    private readonly IInventoryManager _inventoryManager;

    public ProductionPlanService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        IProductionOrderGateway orderGateway,
        IDishRecipeCatalog recipeCatalog,
        IIngredientCatalog ingredientCatalog,
        IInventoryManager inventoryManager)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _orderGateway = orderGateway;
        _recipeCatalog = recipeCatalog;
        _ingredientCatalog = ingredientCatalog;
        _inventoryManager = inventoryManager;
    }

    public async Task<ProductionPlanResponse> GetPlanAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var requirements = await BuildRequirementsAsync(workspaceId, targetDate, cancellationToken);
        var ingredients = await MapIngredientResponsesAsync(
            workspaceId,
            requirements.IngredientTotals,
            cancellationToken);

        return new ProductionPlanResponse(
            targetDate,
            requirements.DishesToCook,
            ingredients);
    }

    public async Task<ExecuteProductionPlanResponse> ExecutePlanAsync(
        ExecuteProductionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var requirements = await BuildRequirementsAsync(
            workspaceId,
            request.TargetDate,
            cancellationToken);

        if (requirements.IngredientTotals.Count == 0 && requirements.DishesToCook.Count == 0)
        {
            throw new KitchenServiceException(
                $"No confirmed orders found for {request.TargetDate:yyyy-MM-dd}.",
                StatusCodes.Status404NotFound);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var availability = await _inventoryManager.CheckStockAvailabilityAsync(
                workspaceId,
                requirements.IngredientTotals,
                cancellationToken);

            if (!availability.IsAvailable)
            {
                var shortages = availability.Shortages
                    .Select(s => new StockShortageResponse(
                        s.IngredientId,
                        s.IngredientName,
                        s.RequiredQuantity,
                        s.AvailableQuantity,
                        s.Unit))
                    .ToList();

                throw new KitchenServiceException(
                    "Insufficient stock for production plan execution.",
                    shortages,
                    StatusCodes.Status409Conflict);
            }

            await _inventoryManager.DeductStockFifoAsync(
                workspaceId,
                requirements.IngredientTotals,
                cancellationToken);

            var ordersUpdated = await _orderGateway.MarkOrdersInProductionAsync(
                workspaceId,
                request.TargetDate,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var ingredientsDeducted = await MapIngredientResponsesAsync(
                workspaceId,
                requirements.IngredientTotals,
                cancellationToken);

            return new ExecuteProductionPlanResponse(
                request.TargetDate,
                ordersUpdated,
                ingredientsDeducted);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ProductionRequirements> BuildRequirementsAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var orderLines = await _orderGateway.GetConfirmedOrderLinesAsync(
            workspaceId,
            targetDate,
            cancellationToken);

        if (orderLines.Count == 0)
        {
            return new ProductionRequirements([], new Dictionary<Guid, decimal>());
        }

        var recipes = await _recipeCatalog.GetRecipesByMenuItemIdsAsync(
            workspaceId,
            orderLines.Select(l => l.MenuItemId),
            cancellationToken);

        var dishTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ingredientTotals = new Dictionary<Guid, decimal>();

        foreach (var line in orderLines)
        {
            if (!recipes.TryGetValue(line.MenuItemId, out var recipe))
            {
                throw new KitchenServiceException(
                    $"Recipe for menu item '{line.MenuItemId}' was not found.",
                    StatusCodes.Status400BadRequest);
            }

            dishTotals.TryGetValue(recipe.DishName, out var currentDishQty);
            dishTotals[recipe.DishName] = currentDishQty + line.Quantity;

            foreach (var ingredient in recipe.Ingredients)
            {
                var required = ingredient.QuantityPerPortion * line.Quantity;
                ingredientTotals.TryGetValue(ingredient.IngredientId, out var current);
                ingredientTotals[ingredient.IngredientId] = current + required;
            }
        }

        var dishesToCook = dishTotals
            .OrderBy(d => d.Key)
            .Select(d => new DishToCookResponse(d.Key, d.Value))
            .ToList();

        return new ProductionRequirements(dishesToCook, ingredientTotals);
    }

    private async Task<IReadOnlyList<IngredientRequiredResponse>> MapIngredientResponsesAsync(
        Guid workspaceId,
        IReadOnlyDictionary<Guid, decimal> ingredientTotals,
        CancellationToken cancellationToken)
    {
        if (ingredientTotals.Count == 0)
        {
            return [];
        }

        var catalog = await _ingredientCatalog.GetByIdsAsync(
            ingredientTotals.Keys,
            workspaceId,
            cancellationToken);

        return ingredientTotals
            .OrderBy(i => catalog.TryGetValue(i.Key, out var info) ? info.Name : i.Key.ToString())
            .Select(i =>
            {
                catalog.TryGetValue(i.Key, out var info);
                return new IngredientRequiredResponse(
                    i.Key,
                    info?.Name ?? "Unknown",
                    i.Value,
                    info?.BaseUnit ?? "Unknown");
            })
            .ToList();
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new KitchenServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }
}
