namespace CateringSaaS.Modules.Inventory.DTOs;

public sealed record IngredientResponse(
    Guid Id,
    string Name,
    string Category,
    string BaseUnit,
    Guid? WorkspaceId,
    bool IsGlobal);
