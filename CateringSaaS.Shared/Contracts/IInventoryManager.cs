namespace CateringSaaS.Shared.Contracts;

public sealed record StockShortage(
    Guid IngredientId,
    string IngredientName,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    string Unit);

public sealed record StockAvailabilityResult(
    bool IsAvailable,
    IReadOnlyList<StockShortage> Shortages);

public interface IInventoryManager
{
    Task<StockAvailabilityResult> CheckStockAvailabilityAsync(
        Guid workspaceId,
        IReadOnlyDictionary<Guid, decimal> requiredIngredients,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>> GetAvailableQuantitiesAsync(
        Guid workspaceId,
        IEnumerable<Guid> ingredientIds,
        CancellationToken cancellationToken = default);

    Task DeductStockFifoAsync(
        Guid workspaceId,
        IReadOnlyDictionary<Guid, decimal> ingredientsToDeduct,
        string? source = null,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
