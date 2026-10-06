namespace CateringSaaS.Shared.Contracts;

public sealed record CriticalStockRow(
    Guid IngredientId,
    string Name,
    string Category,
    string Unit,
    decimal Quantity,
    decimal Threshold);

public sealed record StockMovementDayRow(
    DateOnly Day,
    decimal PurchaseQuantity,
    decimal PurchaseCost,
    decimal ConsumeQuantity,
    decimal ConsumeCost,
    decimal AdjustmentQuantity,
    decimal AdjustmentCost);

public sealed record StockCategorySpendRow(
    string Category,
    decimal PurchaseCost);

public sealed record StockMovementSnapshot(
    decimal PurchaseQuantity,
    decimal PurchaseCost,
    decimal ConsumeQuantity,
    decimal ConsumeCost,
    decimal AdjustmentQuantity,
    decimal AdjustmentCost,
    IReadOnlyList<StockMovementDayRow> Daily,
    IReadOnlyList<StockCategorySpendRow> CategorySpend);

public interface IInventoryReportingQueries
{
    Task<IReadOnlyList<CriticalStockRow>> GetCriticalStockAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<StockMovementSnapshot> GetStockMovementsAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);

    Task<IReadOnlyList<IngredientConsumptionRow>> GetConsumptionByIngredientAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);

    Task<IReadOnlyList<IngredientBalanceRow>> GetIngredientBalancesAsync(
        Guid workspaceId,
        IEnumerable<Guid>? ingredientIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierSpendRow>> GetSupplierSpendAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? supplierId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);
}

public sealed record IngredientConsumptionRow(
    Guid IngredientId,
    string Name,
    string Unit,
    decimal ConsumeQuantity,
    decimal ConsumeCost);

public sealed record IngredientBalanceRow(
    Guid IngredientId,
    string Name,
    string Unit,
    string Category,
    decimal Quantity);

public sealed record SupplierSpendRow(
    Guid SupplierId,
    string SupplierName,
    decimal Spend,
    decimal Quantity,
    int BatchCount);
