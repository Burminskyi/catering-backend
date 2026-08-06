namespace CateringSaaS.Modules.Inventory.DTOs;

public sealed record CreateIngredientRequest(
    string Name,
    string Category,
    string BaseUnit);
