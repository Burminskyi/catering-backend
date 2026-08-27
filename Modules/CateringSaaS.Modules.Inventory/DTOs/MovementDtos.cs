namespace CateringSaaS.Modules.Inventory.DTOs;

public sealed record InventoryMovementListItemResponse(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    string BaseUnit,
    string Type,
    decimal Quantity,
    decimal SignedQuantity,
    decimal TotalCost,
    string Source,
    string? Reason,
    DateTime CreatedAt);

public sealed record PagedInventoryMovementsResponse(
    IReadOnlyList<InventoryMovementListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
