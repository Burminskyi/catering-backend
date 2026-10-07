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
        // Project scalars in SQL (no full OrderItem graph). One business day is small.
        var orders = await _dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.WorkspaceId == workspaceId && o.TargetDate == targetDate)
            .Select(o => new
            {
                o.Id,
                o.ClientCompanyId,
                o.DriverId,
                o.Status,
                o.TotalAmount,
                Portions = o.Items.Sum(i => (int?)i.Quantity) ?? 0
            })
            .ToListAsync(cancellationToken);

        var byStatus = orders
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusCount(
                g.Key.ToString(),
                g.Count(),
                g.Sum(o => o.Portions),
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
                o.Portions,
                o.TotalAmount))
            .ToList();

        var unassignedReady = orders.Count(o =>
            o.Status == OrderStatus.ReadyForDelivery && o.DriverId is null);
        var assignedReady = orders.Count(o =>
            o.Status == OrderStatus.ReadyForDelivery && o.DriverId is not null);

        int Portions(OrderStatus status) =>
            orders.Where(o => o.Status == status).Sum(o => o.Portions);

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
        // Pre-aggregate portions per order in SQL, then group — avoids SelectMany(Items) translation failure.
        var rows = await ActiveOrders(workspaceId, dateFrom, dateTo, clientCompanyId)
            .Select(o => new
            {
                o.ClientCompanyId,
                o.TotalAmount,
                Portions = o.Items.Sum(i => (int?)i.Quantity) ?? 0
            })
            .GroupBy(x => x.ClientCompanyId)
            .Select(g => new
            {
                ClientCompanyId = g.Key,
                OrderCount = g.Count(),
                Portions = g.Sum(x => x.Portions),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(r => r.Revenue)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ClientRevenueRow(r.ClientCompanyId, r.OrderCount, r.Portions, r.Revenue))
            .ToList();
    }

    public async Task<IReadOnlyList<DishPopularityRow>> GetDishPopularityAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var rows = await ActiveOrderItems(workspaceId, dateFrom, dateTo, clientCompanyId)
            .GroupBy(i => new { i.DishName, i.DishId })
            .Select(g => new
            {
                g.Key.DishName,
                g.Key.DishId,
                Portions = g.Sum(i => i.Quantity),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                Revenue = g.Sum(i => i.Subtotal)
            })
            .OrderByDescending(r => r.Portions)
            .ThenBy(r => r.DishName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DishPopularityRow(r.DishName, r.DishId, r.Portions, r.OrderCount, r.Revenue))
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
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status == OrderStatus.Delivered);

        if (clientCompanyId is Guid clientId)
        {
            ordersQuery = ordersQuery.Where(o => o.ClientCompanyId == clientId);
        }

        // Project item fields only — matching reviews + dish labels still need in-memory shaping.
        var orders = await ordersQuery
            .OrderByDescending(o => o.TargetDate)
            .Select(o => new
            {
                o.Id,
                o.ClientCompanyId,
                o.DriverId,
                o.TargetDate,
                o.TotalAmount,
                Items = o.Items.Select(i => new
                {
                    i.MenuItemId,
                    i.DishName,
                    i.Quantity
                }).ToList()
            })
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

        var reviews = await reviewsQuery
            .Select(r => new
            {
                r.Id,
                r.ClientCompanyId,
                r.TargetDate,
                r.MenuItemId,
                r.Rating,
                r.IsReclamation,
                r.Comment
            })
            .ToListAsync(cancellationToken);

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

        var heat = await reviewsQuery
            .GroupBy(r => new { r.ClientCompanyId, r.MenuItemId })
            .Select(g => new
            {
                g.Key.ClientCompanyId,
                g.Key.MenuItemId,
                ReviewCount = g.Count(),
                AvgRating = g.Average(r => (decimal)r.Rating),
                MinRating = g.Min(r => r.Rating)
            })
            .ToListAsync(cancellationToken);

        if (heat.Count == 0)
        {
            return [];
        }

        var menuItemIds = heat.Select(r => r.MenuItemId).Distinct().ToArray();
        var dishNames = await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId && menuItemIds.Contains(i.MenuItemId))
            .GroupBy(i => i.MenuItemId)
            .Select(g => new
            {
                MenuItemId = g.Key,
                DishName = g.Max(i => i.DishName)
            })
            .ToListAsync(cancellationToken);

        var nameByItem = dishNames.ToDictionary(
            x => x.MenuItemId,
            x => string.IsNullOrWhiteSpace(x.DishName) ? "Unknown" : x.DishName);

        return heat
            .Select(r => new ReclamationHeatRow(
                r.ClientCompanyId,
                r.MenuItemId,
                nameByItem.GetValueOrDefault(r.MenuItemId, "Unknown"),
                r.ReviewCount,
                Math.Round(r.AvgRating, 1),
                r.MinRating))
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
        var rows = await ActiveOrderItems(workspaceId, dateFrom, dateTo, clientCompanyId)
            .GroupBy(i => i.MenuItemId)
            .Select(g => new
            {
                MenuItemId = g.Key,
                DishId = g.Max(i => i.DishId),
                DishName = g.Max(i => i.DishName),
                Portions = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(r => r.Portions)
            .ThenBy(r => r.DishName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ProductionDemandLine(
                r.MenuItemId,
                r.DishId,
                string.IsNullOrWhiteSpace(r.DishName) ? "Unknown" : r.DishName,
                r.Portions))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductionDemandLine>> GetConfirmedDemandAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i =>
                i.WorkspaceId == workspaceId
                && i.Order.TargetDate == targetDate
                && i.Order.Status == OrderStatus.Confirmed)
            .GroupBy(i => i.MenuItemId)
            .Select(g => new
            {
                MenuItemId = g.Key,
                DishId = g.Max(i => i.DishId),
                DishName = g.Max(i => i.DishName),
                Portions = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(r => r.Portions)
            .ThenBy(r => r.DishName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ProductionDemandLine(
                r.MenuItemId,
                r.DishId,
                string.IsNullOrWhiteSpace(r.DishName) ? "Unknown" : r.DishName,
                r.Portions))
            .ToList();
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
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status == OrderStatus.Delivered);

        if (driverId is Guid id)
        {
            query = query.Where(o => o.DriverId == id);
        }

        var rows = await query
            .Select(o => new
            {
                o.DriverId,
                o.ClientCompanyId,
                o.TotalAmount,
                Portions = o.Items.Sum(i => (int?)i.Quantity) ?? 0
            })
            .GroupBy(x => x.DriverId)
            .Select(g => new
            {
                DriverId = g.Key,
                OrderCount = g.Count(),
                Portions = g.Sum(x => x.Portions),
                UniqueClients = g.Select(x => x.ClientCompanyId).Distinct().Count(),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(r => r.OrderCount)
            .ThenByDescending(r => r.Portions)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DriverEfficiencyRow(
                r.DriverId,
                r.OrderCount,
                r.Portions,
                r.UniqueClients,
                r.Revenue))
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
            .Where(o =>
                o.WorkspaceId == workspaceId
                && o.TargetDate >= dateFrom
                && o.TargetDate <= dateTo
                && o.Status == OrderStatus.Cancelled);

        if (clientCompanyId is Guid clientId)
        {
            query = query.Where(o => o.ClientCompanyId == clientId);
        }

        var rows = await query
            .Select(o => new
            {
                o.ClientCompanyId,
                o.TargetDate,
                o.TotalAmount,
                Portions = o.Items.Sum(i => (int?)i.Quantity) ?? 0
            })
            .GroupBy(x => new { x.ClientCompanyId, x.TargetDate })
            .Select(g => new
            {
                g.Key.ClientCompanyId,
                g.Key.TargetDate,
                OrderCount = g.Count(),
                Portions = g.Sum(x => x.Portions),
                LostRevenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(r => r.TargetDate)
            .ThenByDescending(r => r.LostRevenue)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new CancellationRow(
                r.ClientCompanyId,
                r.TargetDate,
                "Cancelled",
                r.OrderCount,
                r.Portions,
                r.LostRevenue))
            .ToList();
    }

    private IQueryable<Order> ActiveOrders(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId)
    {
        var query = _dbContext.Set<Order>()
            .AsNoTracking()
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

    private IQueryable<OrderItem> ActiveOrderItems(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId)
    {
        var query = _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i =>
                i.WorkspaceId == workspaceId
                && i.Order.TargetDate >= dateFrom
                && i.Order.TargetDate <= dateTo
                && i.Order.Status != OrderStatus.Cancelled);

        if (clientCompanyId is Guid clientId)
        {
            query = query.Where(i => i.Order.ClientCompanyId == clientId);
        }

        return query;
    }
}
