namespace CateringSaaS.Shared.Contracts;

public sealed record IngredientCatalogItem(
    Guid Id,
    string Name,
    string BaseUnit);

public interface IIngredientCatalog
{
    Task<IReadOnlyDictionary<Guid, IngredientCatalogItem>> GetByIdsAsync(
        IEnumerable<Guid> ingredientIds,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<bool> AreAccessibleAsync(
        IEnumerable<Guid> ingredientIds,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
