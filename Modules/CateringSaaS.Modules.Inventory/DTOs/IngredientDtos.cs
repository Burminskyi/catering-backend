namespace CateringSaaS.Modules.Inventory.DTOs;

public sealed record CreateIngredientRequest(
    string Name,
    string Category,
    string BaseUnit);

public sealed record UpdateIngredientRequest(
    string Name,
    string Category,
    string BaseUnit);

public sealed record IngredientResponse(
    Guid Id,
    string Name,
    string Category,
    string BaseUnit,
    Guid? WorkspaceId,
    bool IsGlobal);

public sealed record PagedIngredientsResponse(
    IReadOnlyList<IngredientResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
