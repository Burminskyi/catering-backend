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

    Task<int> MarkOrdersInProductionAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
