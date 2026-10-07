using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public sealed class OrderReportingQueries : IOrderReportingQueries
{
    private readonly AppDbContext _dbContext;

    public OrderReportingQueries(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TodayOperationsSnapshot> GetTodayOperationsAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.WorkspaceId == workspaceId && o.TargetDate == targetDate)
            .ToListAsync(cancellationToken);

        var byStatus = orders
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusCount(
                g.Key.ToString(),
                g.Count(),
                g.Sum(o => o.Items.Sum(i => i.Quantity)),
                g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount)))
            .OrderBy(x => x.Status)
            .ToList();

        var lines = orders
            .OrderBy(o => o.Status)
            .ThenBy(o => o.ClientCompanyId)
            .Select(o => new TodayOrderLine(
                o.Id,
                o.ClientCompanyId,
                o.DriverId,
                o.Status.ToString(),
                o.Items.Sum(i => i.Quantity),
                o.TotalAmount))
            .ToList();

        var unassignedReady = orders.Count(o =>
            o.Status == OrderStatus.ReadyForDelivery && o.DriverId is null);
        var assignedReady = orders.Count(o =>
            o.Status == OrderStatus.ReadyForDelivery && o.DriverId is not null);

        int Portions(OrderStatus status) =>
            orders.Where(o => o.Status == status).Sum(o => o.Items.Sum(i => i.Quantity));

        var readyPortions = Portions(OrderStatus.ReadyForDelivery) + Portions(OrderStatus.Delivered);
        var inProductionPortions = Portions(OrderStatus.InProduction);
        var waitingPortions = Portions(OrderStatus.Pending) + Portions(OrderStatus.Confirmed);

        return new TodayOperationsSnapshot(
            byStatus,
            lines,
            unassignedReady,
            assignedReady,
            readyPortions,
            inProductionPortions,
            waitingPortions);
    }

    public async Task<IReadOnlyList<ClientRevenueRow>> GetRevenueByClientAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        // Aggregate in memory: EF cannot translate GroupBy + SelectMany(Items).Sum.
        var orders = await ActiveOrders(workspaceId, dateFrom, dateTo, clientCompanyId)
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => o.ClientCompanyId)
            .Select(g => new ClientRevenueRow(
                g.Key,
                g.Count(),
                g.Sum(o => o.Items.Sum(i => i.Quantity)),
                g.Sum(o => o.TotalAmount)))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    public async Task<IReadOnlyList<DishPopularityRow>> GetDishPopularityAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var items = await ActiveOrders(workspaceId, dateFrom, dateTo, clientCompanyId)
            .SelectMany(o => o.Items)
            .Select(i => new
            {
                i.DishName,
                i.DishId,
                i.Quantity,
                i.Subtotal,
                i.OrderId
            })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(i => new { i.DishName, i.DishId })
            .Select(g => new DishPopularityRow(
                g.Key.DishName,
                g.Key.DishId,
                g.Sum(i => i.Quantity),
                g.Select(i => i.OrderId).Distinct().Count(),
                g.Sum(i => i.Subtotal)))
            .OrderByDescending(r => r.Portions)
            .ThenBy(r => r.DishName)
            .ToList();
    }

    public async Task<IReadOnlyList<DeliveryAuditRow>> GetDeliveryAuditAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var ordersQuery = _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status == OrderStatus.Delivered);

        if (clientCompanyId is Guid clientId)
        {
            ordersQuery = ordersQuery.Where(o => o.ClientCompanyId == clientId);
        }

        var orders = await ordersQuery
            .OrderByDescending(o => o.TargetDate)
            .ToListAsync(cancellationToken);

        var reviewsQuery = _dbContext.Set<MealReview>()
            .AsNoTracking()
            .Where(r =>
                r.WorkspaceId == workspaceId
                && r.TargetDate >= dateFrom
                && r.TargetDate <= dateTo);

        if (clientCompanyId is Guid reviewClientId)
        {
            reviewsQuery = reviewsQuery.Where(r => r.ClientCompanyId == reviewClientId);
        }

        var reviews = await reviewsQuery.ToListAsync(cancellationToken);
        var reviewsByKey = reviews
            .GroupBy(r => (r.ClientCompanyId, r.TargetDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        return orders.Select(order =>
        {
            var menuItemIds = order.Items.Select(i => i.MenuItemId).Distinct().ToList();
            reviewsByKey.TryGetValue((order.ClientCompanyId, order.TargetDate), out var dayReviews);
            dayReviews ??= [];

            var matched = dayReviews
                .Where(r => menuItemIds.Contains(r.MenuItemId))
                .Select(r => new DeliveryReviewRow(
                    r.Id,
                    r.MenuItemId,
                    r.Rating,
                    r.IsReclamation,
                    r.Comment))
                .ToList();

            var dishes = string.Join(
                ", ",
                order.Items
                    .GroupBy(i => i.DishName)
                    .Select(g => $"{g.Key} ×{g.Sum(i => i.Quantity)}"));

            return new DeliveryAuditRow(
                order.Id,
                order.ClientCompanyId,
                order.DriverId,
                order.TargetDate,
                order.TotalAmount,
                order.Items.Sum(i => i.Quantity),
                dishes,
                menuItemIds,
                matched);
        }).ToList();
    }

    public async Task<IReadOnlyList<ReclamationHeatRow>> GetReclamationHeatMapAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        int maxRating,
        CancellationToken cancellationToken = default)
    {
        var reviewsQuery = _dbContext.Set<MealReview>()
            .AsNoTracking()
            .Where(r =>
                r.WorkspaceId == workspaceId
                && r.TargetDate >= dateFrom
                && r.TargetDate <= dateTo
                && r.Rating <= maxRating);

        if (clientCompanyId is Guid clientId)
        {
            reviewsQuery = reviewsQuery.Where(r => r.ClientCompanyId == clientId);
        }

        var reviews = await reviewsQuery.ToListAsync(cancellationToken);
        if (reviews.Count == 0)
        {
            return [];
        }

        var menuItemIds = reviews.Select(r => r.MenuItemId).Distinct().ToArray();
        var dishNames = await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId && menuItemIds.Contains(i.MenuItemId))
            .Select(i => new { i.MenuItemId, i.DishName })
            .ToListAsync(cancellationToken);

        var nameByItem = dishNames
            .GroupBy(i => i.MenuItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.DishName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown");

        return reviews
            .GroupBy(r => (r.ClientCompanyId, r.MenuItemId))
            .Select(g => new ReclamationHeatRow(
                g.Key.ClientCompanyId,
                g.Key.MenuItemId,
                nameByItem.GetValueOrDefault(g.Key.MenuItemId, "Unknown"),
                g.Count(),
                Math.Round((decimal)g.Average(r => r.Rating), 1),
                g.Min(r => r.Rating)))
            .OrderBy(r => r.MinRating)
            .ThenByDescending(r => r.ReviewCount)
            .ThenBy(r => r.DishName)
            .ToList();
    }

    public async Task<IReadOnlyList<ProductionDemandLine>> GetProductionDemandAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var items = await ActiveOrders(workspaceId, dateFrom, dateTo, clientCompanyId)
            .SelectMany(o => o.Items)
            .Select(i => new { i.MenuItemId, i.DishId, i.DishName, i.Quantity })
            .ToListAsync(cancellationToken);

        return GroupDemand(items.Select(i => (i.MenuItemId, i.DishId, i.DishName, i.Quantity)));
    }

    public async Task<IReadOnlyList<ProductionDemandLine>> GetConfirmedDemandAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i =>
                i.WorkspaceId == workspaceId
                && i.Order.TargetDate == targetDate
                && i.Order.Status == OrderStatus.Confirmed)
            .Select(i => new { i.MenuItemId, i.DishId, i.DishName, i.Quantity })
            .ToListAsync(cancellationToken);

        return GroupDemand(items.Select(i => (i.MenuItemId, i.DishId, i.DishName, i.Quantity)));
    }

    public async Task<IReadOnlyList<DriverEfficiencyRow>> GetDriverEfficiencyAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? driverId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status == OrderStatus.Delivered);

        if (driverId is Guid id)
        {
            query = query.Where(o => o.DriverId == id);
        }

        var orders = await query.ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => o.DriverId)
            .Select(g => new DriverEfficiencyRow(
                g.Key,
                g.Count(),
                g.Sum(o => o.Items.Sum(i => i.Quantity)),
                g.Select(o => o.ClientCompanyId).Distinct().Count(),
                g.Sum(o => o.TotalAmount)))
            .OrderByDescending(r => r.OrderCount)
            .ThenByDescending(r => r.Portions)
            .ToList();
    }

    public async Task<IReadOnlyList<CancellationRow>> GetCancellationsAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status == OrderStatus.Cancelled);

        if (clientCompanyId is Guid clientId)
        {
            query = query.Where(o => o.ClientCompanyId == clientId);
        }

        var orders = await query.ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => new { o.ClientCompanyId, o.TargetDate })
            .Select(g => new CancellationRow(
                g.Key.ClientCompanyId,
                g.Key.TargetDate,
                "Cancelled",
                g.Count(),
                g.Sum(o => o.Items.Sum(i => i.Quantity)),
                g.Sum(o => o.TotalAmount)))
            .OrderByDescending(r => r.TargetDate)
            .ThenByDescending(r => r.LostRevenue)
            .ToList();
    }

    private static IReadOnlyList<ProductionDemandLine> GroupDemand(
        IEnumerable<(Guid MenuItemId, Guid? DishId, string DishName, int Quantity)> items) =>
        items
            .GroupBy(i => i.MenuItemId)
            .Select(g => new ProductionDemandLine(
                g.Key,
                g.Select(i => i.DishId).FirstOrDefault(id => id is not null),
                g.Select(i => i.DishName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown",
                g.Sum(i => i.Quantity)))
            .OrderByDescending(r => r.Portions)
            .ThenBy(r => r.DishName)
            .ToList();

    private IQueryable<Order> ActiveOrders(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId)
    {
        var query = _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status != OrderStatus.Cancelled);

        if (clientCompanyId is Guid clientId)
        {
            query = query.Where(o => o.ClientCompanyId == clientId);
        }

        return query;
    }
}
