using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public sealed class ProductionOrderGateway : IProductionOrderGateway
{
    private readonly AppDbContext _dbContext;

    public ProductionOrderGateway(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductionOrderItemLine>> GetConfirmedOrderLinesAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i =>
                i.WorkspaceId == workspaceId
                && i.Order.TargetDate == targetDate
                && i.Order.Status == OrderStatus.Confirmed)
            .Select(i => new ProductionOrderItemLine(
                i.OrderId,
                i.Id,
                i.MenuItemId,
                i.Quantity))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionOrderItemLine>> LockConfirmedOrdersAndGetUnconsumedLinesAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Confirmed order rows must be locked inside a transaction.");
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SELECT 1 FROM orders
            WHERE "WorkspaceId" = {workspaceId}
              AND "TargetDate" = {targetDate}
              AND "Status" = 'Confirmed'
            FOR UPDATE
            """,
            cancellationToken);

        return await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i =>
                i.WorkspaceId == workspaceId
                && i.Order.TargetDate == targetDate
                && i.Order.Status == OrderStatus.Confirmed
                && i.Order.StockConsumedAt == null)
            .Select(i => new ProductionOrderItemLine(
                i.OrderId,
                i.Id,
                i.MenuItemId,
                i.Quantity))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> MarkOrdersInProductionAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Set<Order>()
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate == targetDate
                && o.Status == OrderStatus.Confirmed
                && o.StockConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            order.Status = OrderStatus.InProduction;
            // Kitchen plan already deducted FIFO for these orders in the same transaction.
            order.StockConsumedAt ??= DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return orders.Count;
    }
}
