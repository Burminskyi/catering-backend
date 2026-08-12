namespace CateringSaaS.Shared.Contracts;

public sealed record DishRecipeIngredientLine(
    Guid IngredientId,
    decimal QuantityPerPortion);

public sealed record MenuItemRecipeSnapshot(
    Guid MenuItemId,
    Guid DishId,
    string DishName,
    IReadOnlyList<DishRecipeIngredientLine> Ingredients);

public interface IDishRecipeCatalog
{
    Task<IReadOnlyDictionary<Guid, MenuItemRecipeSnapshot>> GetRecipesByMenuItemIdsAsync(
        Guid workspaceId,
        IEnumerable<Guid> menuItemIds,
        CancellationToken cancellationToken = default);
}
