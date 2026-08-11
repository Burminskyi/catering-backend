namespace CateringSaaS.Modules.Menu.DTOs;

public sealed record DishIngredientInput(Guid IngredientId, decimal Quantity);

public sealed record DishIngredientResponse(
    Guid IngredientId,
    string IngredientName,
    string BaseUnit,
    decimal Quantity);

public sealed record DishResponse(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Description,
    string Category,
    int OutputWeight,
    string? Instructions,
    bool IsActive,
    IReadOnlyList<DishIngredientResponse> Ingredients);

public sealed record CreateDishRequest(
    string Name,
    string? Description,
    string Category,
    int OutputWeight,
    string? Instructions,
    IReadOnlyList<DishIngredientInput> Ingredients);

public sealed record UpdateDishRequest(
    string Name,
    string? Description,
    string Category,
    int OutputWeight,
    string? Instructions,
    IReadOnlyList<DishIngredientInput> Ingredients);
