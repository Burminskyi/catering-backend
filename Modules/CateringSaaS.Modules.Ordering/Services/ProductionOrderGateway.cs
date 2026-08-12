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

    public async Task<int> MarkOrdersInProductionAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Set<Order>()
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate == targetDate
                && o.Status == OrderStatus.Confirmed)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            order.Status = OrderStatus.InProduction;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return orders.Count;
    }
}
