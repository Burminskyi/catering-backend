namespace CateringSaaS.Modules.Inventory.DTOs;

public sealed record AddStockPurchaseRequest(
    Guid IngredientId,
    Guid SupplierId,
    decimal Quantity,
    string Unit,
    decimal TotalCost,
    DateTime? PurchasedAt);

public sealed record StockPurchaseResponse(
    Guid BatchId,
    Guid IngredientId,
    string IngredientName,
    Guid SupplierId,
    string SupplierName,
    decimal QuantityInBaseUnits,
    string BaseUnit,
    decimal TotalCost,
    decimal UnitCost,
    DateTime ReceivedAt,
    decimal InventoryTotalQuantity);

public sealed record ConsumeStockRequest(
    Guid IngredientId,
    decimal Quantity,
    string Unit,
    string? Reason);

public sealed record ConsumeStockBatchAllocation(
    Guid BatchId,
    decimal QuantityConsumed,
    decimal CostAllocated);

public sealed record ConsumeStockResponse(
    Guid IngredientId,
    string IngredientName,
    decimal QuantityConsumedInBaseUnits,
    string BaseUnit,
    decimal TotalWriteOffCost,
    decimal RemainingInventoryQuantity,
    IReadOnlyList<ConsumeStockBatchAllocation> Allocations);

public sealed record InventoryBalanceItemResponse(
    Guid IngredientId,
    string IngredientName,
    string Category,
    string BaseUnit,
    decimal TotalQuantity,
    decimal WeightedAverageUnitCost,
    decimal TotalStockValue,
    int ActiveBatchCount);

public sealed record InventoryBalanceResponse(
    Guid WorkspaceId,
    IReadOnlyList<InventoryBalanceItemResponse> Items,
    decimal GrandTotalValue);
