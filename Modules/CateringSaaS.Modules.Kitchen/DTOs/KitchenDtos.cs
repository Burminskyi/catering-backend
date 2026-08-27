namespace CateringSaaS.Modules.Kitchen.DTOs;

public sealed record DishToCookResponse(string DishName, int TotalPortions);

public sealed record IngredientRequiredResponse(
    Guid IngredientId,
    string IngredientName,
    decimal TotalQuantity,
    string Unit);

public sealed record ProductionPlanResponse(
    DateOnly TargetDate,
    IReadOnlyList<DishToCookResponse> DishesToCook,
    IReadOnlyList<IngredientRequiredResponse> IngredientsRequired);

public sealed record ExecuteProductionPlanRequest(DateOnly TargetDate);

public sealed record ExecuteProductionPlanResponse(
    DateOnly TargetDate,
    int OrdersMovedToInProduction,
    IReadOnlyList<IngredientRequiredResponse> IngredientsDeducted);

public sealed record StockShortageResponse(
    Guid IngredientId,
    string IngredientName,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    string Unit);

public sealed record ShoppingListItemResponse(
    Guid IngredientId,
    string IngredientName,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal ToBuyQuantity,
    string Unit);

public sealed record ShoppingListResponse(
    DateOnly TargetDate,
    IReadOnlyList<ShoppingListItemResponse> Items);
