namespace CateringSaaS.Shared.Contracts;

public sealed record ProductionOrderItemLine(
    Guid OrderId,
    Guid OrderItemId,
    Guid MenuItemId,
    int Quantity);

public interface IProductionOrderGateway
{
    Task<IReadOnlyList<ProductionOrderItemLine>> GetConfirmedOrderLinesAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks confirmed orders for the date, then returns lines that have not yet consumed stock.
    /// Must run inside an open transaction so the row locks are held through deduct + status update.
    /// </summary>
    Task<IReadOnlyList<ProductionOrderItemLine>> LockConfirmedOrdersAndGetUnconsumedLinesAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    Task<int> MarkOrdersInProductionAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
