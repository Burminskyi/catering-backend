using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.MultiTenancy;
using CateringSaaS.Shared.Reporting;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Reporting.Services;

public interface IReportingService
{
    Task<ReportResponse> GetTodayPulseAsync(
        DateOnly? targetDate,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetRevenueByClientAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetStockMovementsAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);

    Task<ReportResponse> GetDeliveryAuditAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetDishPopularityAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetReclamationHeatMapAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        int? maxRating,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetConsumptionVarianceAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);

    Task<ReportResponse> GetFoodCostAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);

    Task<ReportResponse> GetShortageForecastAsync(
        DateOnly? targetDate,
        bool? onlyDeficits,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetDriverEfficiencyAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? driverId,
        CancellationToken cancellationToken = default);

    Task<ReportResponse> GetSupplierSpendAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? supplierId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null);

    Task<ReportResponse> GetCancellationsAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);
}

public sealed class ReportingService : IReportingService
{
    private readonly ITenantContext _tenantContext;
    private readonly IClientTimeContext _clock;
    private readonly IOrderReportingQueries _orders;
    private readonly IInventoryReportingQueries _inventory;
    private readonly IClientCompanyLookup _clients;
    private readonly IUserDisplayLookup _users;
    private readonly IDishRecipeCatalog _recipes;
    private readonly IIngredientCatalog _ingredients;

    public ReportingService(
        ITenantContext tenantContext,
        IClientTimeContext clock,
        IOrderReportingQueries orders,
        IInventoryReportingQueries inventory,
        IClientCompanyLookup clients,
        IUserDisplayLookup users,
        IDishRecipeCatalog recipes,
        IIngredientCatalog ingredients)
    {
        _tenantContext = tenantContext;
        _clock = clock;
        _orders = orders;
        _inventory = inventory;
        _clients = clients;
        _users = users;
        _recipes = recipes;
        _ingredients = ingredients;
    }

    public async Task<ReportResponse> GetTodayPulseAsync(
        DateOnly? targetDate,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var day = targetDate ?? _clock.LocalToday;

        var snapshot = await _orders.GetTodayOperationsAsync(workspaceId, day, cancellationToken);
        var critical = await _inventory.GetCriticalStockAsync(workspaceId, cancellationToken);

        var clientIds = snapshot.Orders.Select(o => o.ClientCompanyId);
        var driverIds = snapshot.Orders.Where(o => o.DriverId is not null).Select(o => o.DriverId!.Value);
        var contacts = await _clients.GetContactsAsync(workspaceId, clientIds, cancellationToken);
        var drivers = await _users.GetDisplayNamesAsync(driverIds, cancellationToken);

        var active = snapshot.Orders.Where(o => o.Status != "Cancelled").ToList();
        var revenue = active.Sum(o => o.TotalAmount);
        var portions = active.Sum(o => o.Portions);
        var pending = CountStatus(snapshot, "Pending");
        var readiness = portions > 0
            ? Math.Round(100m * snapshot.ReadyPortions / portions, 1)
            : 0m;

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("ordersCount", ReportLabels.OrdersToday, active.Count, active.Count.ToString(),
                ReportLabels.PendingConfirmation(pending), pending.ToString()),
            ReportComposer.Metric("revenue", ReportLabels.RevenueToday, revenue, ReportComposer.Money(revenue),
                ReportLabels.ActiveOrders(active.Count)),
            ReportComposer.Metric("portions", ReportLabels.PortionsToday, portions, portions.ToString(),
                ReportLabels.ReadyCount(snapshot.ReadyPortions)),
            ReportComposer.Metric("assignedReadyCount", ReportLabels.AssignedForDelivery, snapshot.AssignedReadyCount,
                snapshot.AssignedReadyCount.ToString()),
            ReportComposer.Metric("unassignedReadyCount", ReportLabels.UnassignedReadyOrders, snapshot.UnassignedReadyCount,
                snapshot.UnassignedReadyCount.ToString()),
            ReportComposer.Metric("criticalStockCount", ReportLabels.CriticalStockItems, critical.Count,
                critical.Count.ToString()),
            ReportComposer.Metric("readyPortions", ReportLabels.ReadyPortions, snapshot.ReadyPortions,
                snapshot.ReadyPortions.ToString()),
            ReportComposer.Metric("inProductionPortions", ReportLabels.InProductionPortions, snapshot.InProductionPortions,
                snapshot.InProductionPortions.ToString()),
            ReportComposer.Metric("waitingPortions", ReportLabels.WaitingPortions, snapshot.WaitingPortions,
                snapshot.WaitingPortions.ToString()),
            ReportComposer.Metric("readinessPercent", ReportLabels.KitchenReadiness, readiness,
                ReportComposer.Percent(readiness))
        };

        foreach (var status in snapshot.ByStatus)
        {
            metrics.Add(ReportComposer.Metric(
                $"status.{status.Status}",
                status.Status,
                status.OrderCount,
                status.OrderCount.ToString(),
                ReportLabels.NPortions(status.Portions)));
        }

        var orderRows = snapshot.Orders.Select(o =>
        {
            contacts.TryGetValue(o.ClientCompanyId, out var client);
            var driverName = o.DriverId is Guid driverId && drivers.TryGetValue(driverId, out var name)
                ? name
                : null;
            return ReportComposer.Row(
                ("id", o.OrderId),
                ("clientName", client?.Name ?? o.ClientCompanyId.ToString()),
                ("portions", o.Portions),
                ("status", o.Status),
                ("revenue", o.TotalAmount),
                ("driverName", driverName),
                ("assigned", o.DriverId is not null));
        }).ToList();

        var stockRows = critical.Select(s =>
        {
            var percent = s.Threshold > 0
                ? Math.Round(100m * s.Quantity / s.Threshold, 0)
                : 0m;
            return ReportComposer.Row(
                ("ingredientId", s.IngredientId),
                ("name", s.Name),
                ("quantity", s.Quantity),
                ("unit", s.Unit),
                ("threshold", s.Threshold),
                ("percent", percent),
                ("category", s.Category));
        }).ToList();

        var tables = new List<ReportTable>
        {
            new(
                "todayOrders",
                ReportLabels.TodaysOrders,
                [
                    new ReportColumn("clientName", ReportLabels.Client),
                    new ReportColumn("portions", ReportLabels.Portions, "number"),
                    new ReportColumn("status", ReportLabels.Status, "status"),
                    new ReportColumn("driverName", ReportLabels.Driver)
                ],
                orderRows),
            new(
                "criticalStock",
                ReportLabels.CriticalStock,
                [
                    new ReportColumn("name", ReportLabels.Ingredient),
                    new ReportColumn("quantity", ReportLabels.OnHand, "number"),
                    new ReportColumn("unit", ReportLabels.Unit),
                    new ReportColumn("threshold", ReportLabels.Min, "number")
                ],
                stockRows)
        };

        var series = new List<ReportSeries>
        {
            new(
                "ordersByStatus",
                ReportLabels.OrdersByStatus,
                "bar",
                snapshot.ByStatus.Select(s => new ReportSeriesPoint(s.Status, s.OrderCount)).ToList())
        };

        return new ReportResponse("todayPulse", ReportTitles.TodayPulse, day, day, metrics, tables, series);
    }

    public async Task<ReportResponse> GetRevenueByClientAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var rows = await _orders.GetRevenueByClientAsync(
            workspaceId, from, to, clientCompanyId, cancellationToken);
        var contacts = await _clients.GetContactsAsync(
            workspaceId, rows.Select(r => r.ClientCompanyId), cancellationToken);

        var revenue = rows.Sum(r => r.Revenue);
        var portions = rows.Sum(r => r.Portions);
        var orders = rows.Sum(r => r.OrderCount);

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("revenue", ReportLabels.Revenue, revenue, ReportComposer.Money(revenue)),
            ReportComposer.Metric("portions", ReportLabels.Portions, portions, portions.ToString()),
            ReportComposer.Metric("orderCount", ReportLabels.Orders, orders, orders.ToString()),
            ReportComposer.Metric("clientCount", ReportLabels.Clients, rows.Count, rows.Count.ToString())
        };

        var tableRows = rows.Select(r =>
        {
            contacts.TryGetValue(r.ClientCompanyId, out var client);
            return ReportComposer.Row(
                ("clientCompanyId", r.ClientCompanyId),
                ("clientName", client?.Name ?? r.ClientCompanyId.ToString()),
                ("orderCount", r.OrderCount),
                ("portions", r.Portions),
                ("revenue", r.Revenue));
        }).ToList();

        var tables = new List<ReportTable>
        {
            new(
                "revenueByClient",
                ReportLabels.RevenueByClient,
                [
                    new ReportColumn("clientName", ReportLabels.Client),
                    new ReportColumn("orderCount", ReportLabels.Orders, "number"),
                    new ReportColumn("portions", ReportLabels.Portions, "number"),
                    new ReportColumn("revenue", ReportLabels.Revenue, "money")
                ],
                tableRows)
        };

        var series = new List<ReportSeries>
        {
            new(
                "revenueByClient",
                ReportLabels.RevenueByClient,
                "bar",
                rows.Take(12).Select(r =>
                {
                    contacts.TryGetValue(r.ClientCompanyId, out var client);
                    return new ReportSeriesPoint(client?.Name ?? ReportLabels.Client, r.Revenue);
                }).ToList())
        };

        return new ReportResponse("revenueByClient", ReportTitles.RevenueByClient, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetStockMovementsAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var snapshot = await _inventory.GetStockMovementsAsync(
            workspaceId, from, to, ingredientId, cancellationToken, timeFrom, timeTo);

        var usedShare = snapshot.PurchaseQuantity > 0
            ? Math.Round(100m * snapshot.ConsumeQuantity / snapshot.PurchaseQuantity, 1)
            : 0m;

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("purchaseCost", ReportLabels.Purchased, snapshot.PurchaseCost,
                ReportComposer.Money(snapshot.PurchaseCost),
                $"{ReportComposer.Quantity(snapshot.PurchaseQuantity)} units"),
            ReportComposer.Metric("consumeCost", ReportLabels.Used, snapshot.ConsumeCost,
                ReportComposer.Money(snapshot.ConsumeCost),
                $"{ReportComposer.Quantity(snapshot.ConsumeQuantity)} units"),
            ReportComposer.Metric("usedShare", ReportLabels.UsedShare, usedShare, ReportComposer.Percent(usedShare)),
            ReportComposer.Metric("adjustmentCost", ReportLabels.Adjustments, snapshot.AdjustmentCost,
                ReportComposer.Money(snapshot.AdjustmentCost),
                $"{ReportComposer.Quantity(snapshot.AdjustmentQuantity)} units")
        };

        var tableRows = snapshot.Daily.Select(d => ReportComposer.Row(
            ("day", d.Day.ToString("yyyy-MM-dd")),
            ("purchaseQuantity", d.PurchaseQuantity),
            ("purchaseCost", d.PurchaseCost),
            ("consumeQuantity", d.ConsumeQuantity),
            ("consumeCost", d.ConsumeCost),
            ("adjustmentQuantity", d.AdjustmentQuantity),
            ("adjustmentCost", d.AdjustmentCost))).ToList();

        var series = new List<ReportSeries>
        {
            new(
                "purchases",
                ReportLabels.Purchased,
                "bar",
                snapshot.Daily.Select(d => new ReportSeriesPoint(d.Day.ToString("MM-dd"), d.PurchaseQuantity, "purchased")).ToList()),
            new(
                "usage",
                ReportLabels.Used,
                "bar",
                snapshot.Daily.Select(d => new ReportSeriesPoint(d.Day.ToString("MM-dd"), d.ConsumeQuantity, "used")).ToList()),
            new(
                "categorySpend",
                ReportLabels.SpendByCategory,
                "pie",
                snapshot.CategorySpend.Select(c => new ReportSeriesPoint(c.Category, c.PurchaseCost)).ToList())
        };

        var tables = new List<ReportTable>
        {
            new(
                "stockMovements",
                ReportLabels.DailyStockMovements,
                [
                    new ReportColumn("day", ReportLabels.Day),
                    new ReportColumn("purchaseQuantity", ReportLabels.PurchasedQty, "number"),
                    new ReportColumn("purchaseCost", ReportLabels.PurchasedCost, "money"),
                    new ReportColumn("consumeQuantity", ReportLabels.UsedQty, "number"),
                    new ReportColumn("consumeCost", ReportLabels.UsedCost, "money"),
                    new ReportColumn("adjustmentCost", ReportLabels.Adjustments, "money")
                ],
                tableRows)
        };

        return new ReportResponse("stockMovements", ReportTitles.StockMovements, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetDeliveryAuditAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var rows = await _orders.GetDeliveryAuditAsync(
            workspaceId, from, to, clientCompanyId, cancellationToken);

        var contacts = await _clients.GetContactsAsync(
            workspaceId, rows.Select(r => r.ClientCompanyId), cancellationToken);
        var drivers = await _users.GetDisplayNamesAsync(
            rows.Where(r => r.DriverId is not null).Select(r => r.DriverId!.Value),
            cancellationToken);

        var reclamations = rows.SelectMany(r => r.Reviews).Count(r => r.IsReclamation);
        var reviewCount = rows.SelectMany(r => r.Reviews).Count();
        var avgRating = reviewCount > 0
            ? Math.Round((decimal)rows.SelectMany(r => r.Reviews).Average(r => r.Rating), 1)
            : 0m;

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("deliveredOrders", ReportLabels.DeliveredOrders, rows.Count, rows.Count.ToString()),
            ReportComposer.Metric("reviewCount", ReportLabels.Reviews, reviewCount, reviewCount.ToString()),
            ReportComposer.Metric("reclamationCount", ReportLabels.Reclamations, reclamations, reclamations.ToString()),
            ReportComposer.Metric("averageRating", ReportLabels.AverageRating, avgRating, avgRating.ToString("0.0"))
        };

        var tableRows = rows.Select(r =>
        {
            contacts.TryGetValue(r.ClientCompanyId, out var client);
            var driverName = r.DriverId is Guid driverId && drivers.TryGetValue(driverId, out var name)
                ? name
                : null;
            var worst = r.Reviews.OrderBy(v => v.Rating).FirstOrDefault();
            var comments = string.Join(
                " | ",
                r.Reviews.Where(v => !string.IsNullOrWhiteSpace(v.Comment)).Select(v => v.Comment));
            return ReportComposer.Row(
                ("orderId", r.OrderId),
                ("targetDate", r.TargetDate.ToString("yyyy-MM-dd")),
                ("clientName", client?.Name ?? r.ClientCompanyId.ToString()),
                ("driverName", driverName),
                ("dishes", r.Dishes),
                ("portions", r.Portions),
                ("rating", worst?.Rating),
                ("isReclamation", r.Reviews.Any(v => v.IsReclamation)),
                ("comment", string.IsNullOrWhiteSpace(comments) ? null : comments));
        }).ToList();

        var tables = new List<ReportTable>
        {
            new(
                "deliveryAudit",
                ReportLabels.DeliveredOrdersReviews,
                [
                    new ReportColumn("targetDate", ReportLabels.Date),
                    new ReportColumn("clientName", ReportLabels.Client),
                    new ReportColumn("driverName", ReportLabels.Driver),
                    new ReportColumn("dishes", ReportLabels.Dishes),
                    new ReportColumn("rating", ReportLabels.Rating, "number"),
                    new ReportColumn("comment", ReportLabels.Comments)
                ],
                tableRows)
        };

        IReadOnlyList<ReportSeries> series =
        [
            new ReportSeries(
                "reclamations",
                ReportLabels.ReviewMix,
                "pie",
                [
                    new ReportSeriesPoint(ReportLabels.Reclamations, reclamations),
                    new ReportSeriesPoint(ReportLabels.OtherReviews, Math.Max(0, reviewCount - reclamations))
                ])
        ];

        return new ReportResponse("deliveryAudit", ReportTitles.DeliveryAudit, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetDishPopularityAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var rows = await _orders.GetDishPopularityAsync(
            workspaceId, from, to, clientCompanyId, cancellationToken);

        var portions = rows.Sum(r => r.Portions);
        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("dishCount", ReportLabels.Dishes, rows.Count, rows.Count.ToString()),
            ReportComposer.Metric("portions", ReportLabels.PortionsCooked, portions, portions.ToString()),
            ReportComposer.Metric("revenue", ReportLabels.DishRevenue, rows.Sum(r => r.Revenue),
                ReportComposer.Money(rows.Sum(r => r.Revenue)))
        };

        var tableRows = rows.Select(r => ReportComposer.Row(
            ("dishName", r.DishName),
            ("dishId", r.DishId),
            ("portions", r.Portions),
            ("orderCount", r.OrderCount),
            ("revenue", r.Revenue))).ToList();

        var tables = new List<ReportTable>
        {
            new(
                "dishPopularity",
                ReportLabels.TopDishes,
                [
                    new ReportColumn("dishName", ReportLabels.Dish),
                    new ReportColumn("portions", ReportLabels.Portions, "number"),
                    new ReportColumn("orderCount", ReportLabels.Orders, "number"),
                    new ReportColumn("revenue", ReportLabels.Revenue, "money")
                ],
                tableRows)
        };

        var series = new List<ReportSeries>
        {
            new(
                "dishPortions",
                ReportLabels.PortionsByDish,
                "bar",
                rows.Take(12).Select(r => new ReportSeriesPoint(
                    string.IsNullOrWhiteSpace(r.DishName) ? ReportLabels.Dish : r.DishName,
                    r.Portions)).ToList())
        };

        return new ReportResponse("dishPopularity", ReportTitles.DishPopularity, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetReclamationHeatMapAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        int? maxRating,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var cap = Math.Clamp(maxRating ?? 3, 1, 5);
        var rows = await _orders.GetReclamationHeatMapAsync(
            workspaceId, from, to, clientCompanyId, cap, cancellationToken);
        var contacts = await _clients.GetContactsAsync(
            workspaceId, rows.Select(r => r.ClientCompanyId), cancellationToken);

        var reviewCount = rows.Sum(r => r.ReviewCount);
        var avg = reviewCount > 0
            ? Math.Round(rows.Sum(r => r.AverageRating * r.ReviewCount) / reviewCount, 1)
            : 0m;

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("reclamationCount", ReportLabels.Reclamations, reviewCount, reviewCount.ToString(),
                $"rating ≤ {cap}"),
            ReportComposer.Metric("averageRating", ReportLabels.AverageRating, avg, avg.ToString("0.0")),
            ReportComposer.Metric("dishCount", ReportLabels.Dishes, rows.Select(r => r.MenuItemId).Distinct().Count(),
                rows.Select(r => r.MenuItemId).Distinct().Count().ToString()),
            ReportComposer.Metric("clientCount", ReportLabels.Clients, rows.Select(r => r.ClientCompanyId).Distinct().Count(),
                rows.Select(r => r.ClientCompanyId).Distinct().Count().ToString())
        };

        var tableRows = rows.Select(r =>
        {
            contacts.TryGetValue(r.ClientCompanyId, out var client);
            return ReportComposer.Row(
                ("clientCompanyId", r.ClientCompanyId),
                ("clientName", client?.Name ?? r.ClientCompanyId.ToString()),
                ("dishName", r.DishName),
                ("reviewCount", r.ReviewCount),
                ("averageRating", r.AverageRating),
                ("minRating", r.MinRating),
                ("isReclamation", r.MinRating <= 3));
        }).ToList();

        var byDish = rows
            .GroupBy(r => r.DishName)
            .Select(g => new ReportSeriesPoint(g.Key, g.Sum(x => x.ReviewCount)))
            .OrderByDescending(p => p.Value)
            .Take(12)
            .ToList();

        var tables = new List<ReportTable>
        {
            new(
                "reclamationHeatMap",
                ReportLabels.ReclamationsByClientDish,
                [
                    new ReportColumn("clientName", ReportLabels.Client),
                    new ReportColumn("dishName", ReportLabels.Dish),
                    new ReportColumn("reviewCount", ReportLabels.Reviews, "number"),
                    new ReportColumn("averageRating", ReportLabels.AvgRating, "number"),
                    new ReportColumn("minRating", ReportLabels.Worst, "number")
                ],
                tableRows)
        };

        IReadOnlyList<ReportSeries> series =
        [
            new ReportSeries("reclamationsByDish", ReportLabels.ReclamationsByDish, "bar", byDish)
        ];

        return new ReportResponse("reclamationHeatMap", ReportTitles.ReclamationHeatMap, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetConsumptionVarianceAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var demand = await _orders.GetProductionDemandAsync(
            workspaceId, from, to, clientCompanyId: null, cancellationToken);
        var expected = await ExpandExpectedUsageAsync(workspaceId, demand, cancellationToken);
        if (ingredientId is Guid filterId)
        {
            expected = expected
                .Where(kv => kv.Key == filterId)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        var actual = await _inventory.GetConsumptionByIngredientAsync(
            workspaceId, from, to, ingredientId, cancellationToken, timeFrom, timeTo);
        var actualById = actual.ToDictionary(r => r.IngredientId);

        var ids = expected.Keys.Concat(actualById.Keys).Distinct().ToArray();
        var catalog = await _ingredients.GetByIdsAsync(ids, workspaceId, cancellationToken);

        var tableRows = new List<Dictionary<string, object?>>();
        foreach (var id in ids.OrderBy(x => catalog.TryGetValue(x, out var info) ? info.Name : x.ToString()))
        {
            catalog.TryGetValue(id, out var info);
            actualById.TryGetValue(id, out var consumed);
            expected.TryGetValue(id, out var expectedQty);
            var actualQty = consumed?.ConsumeQuantity ?? 0m;
            var variance = actualQty - expectedQty;
            var variancePct = expectedQty > 0 ? Math.Round(100m * variance / expectedQty, 1) : (decimal?)null;
            tableRows.Add(ReportComposer.Row(
                ("ingredientId", id),
                ("name", info?.Name ?? consumed?.Name ?? id.ToString()),
                ("unit", info?.BaseUnit ?? consumed?.Unit),
                ("expectedQuantity", expectedQty),
                ("actualQuantity", actualQty),
                ("varianceQuantity", variance),
                ("variancePercent", variancePct),
                ("consumeCost", consumed?.ConsumeCost ?? 0m),
                ("isOver", variance > 0)));
        }

        var overCount = tableRows.Count(r => r.TryGetValue("isOver", out var flag) && flag is true);
        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("ingredientCount", ReportLabels.Ingredients, tableRows.Count, tableRows.Count.ToString()),
            ReportComposer.Metric("expectedQuantity", ReportLabels.ExpectedUsage, expected.Values.Sum(),
                ReportComposer.Quantity(expected.Values.Sum())),
            ReportComposer.Metric("actualQuantity", ReportLabels.ActualConsumption, actual.Sum(r => r.ConsumeQuantity),
                ReportComposer.Quantity(actual.Sum(r => r.ConsumeQuantity))),
            ReportComposer.Metric("overCount", ReportLabels.OverConsumed, overCount, overCount.ToString())
        };

        var series = new List<ReportSeries>
        {
            new(
                "expected",
                ReportLabels.Expected,
                "bar",
                tableRows.Take(12).Select(r => new ReportSeriesPoint(
                    Convert.ToString(r["name"]) ?? ReportLabels.Ingredient,
                    Convert.ToDecimal(r["expectedQuantity"] ?? 0m),
                    "expected")).ToList()),
            new(
                "actual",
                ReportLabels.Actual,
                "bar",
                tableRows.Take(12).Select(r => new ReportSeriesPoint(
                    Convert.ToString(r["name"]) ?? ReportLabels.Ingredient,
                    Convert.ToDecimal(r["actualQuantity"] ?? 0m),
                    "actual")).ToList())
        };

        var tables = new List<ReportTable>
        {
            new(
                "consumptionVariance",
                ReportLabels.ExpectedVsActual,
                [
                    new ReportColumn("name", ReportLabels.Ingredient),
                    new ReportColumn("unit", ReportLabels.Unit),
                    new ReportColumn("expectedQuantity", ReportLabels.Expected, "number"),
                    new ReportColumn("actualQuantity", ReportLabels.Actual, "number"),
                    new ReportColumn("varianceQuantity", ReportLabels.Variance, "number"),
                    new ReportColumn("variancePercent", ReportLabels.VariancePercent, "number")
                ],
                tableRows)
        };

        return new ReportResponse("consumptionVariance", ReportTitles.ConsumptionVariance, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetFoodCostAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var stock = await _inventory.GetStockMovementsAsync(
            workspaceId, from, to, null, cancellationToken, timeFrom, timeTo);
        var revenueRows = await _orders.GetRevenueByClientAsync(workspaceId, from, to, null, cancellationToken);
        var revenue = revenueRows.Sum(r => r.Revenue);
        var foodCost = revenue > 0 ? Math.Round(100m * stock.ConsumeCost / revenue, 1) : 0m;
        var usedShare = stock.PurchaseCost > 0
            ? Math.Round(100m * stock.ConsumeCost / stock.PurchaseCost, 1)
            : 0m;

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("purchaseCost", ReportLabels.Purchased, stock.PurchaseCost,
                ReportComposer.Money(stock.PurchaseCost)),
            ReportComposer.Metric("consumeCost", ReportLabels.Consumed, stock.ConsumeCost,
                ReportComposer.Money(stock.ConsumeCost)),
            ReportComposer.Metric("revenue", ReportLabels.OrderRevenue, revenue, ReportComposer.Money(revenue)),
            ReportComposer.Metric("foodCostPercent", ReportLabels.FoodCostPercent, foodCost, ReportComposer.Percent(foodCost),
                ReportLabels.ConsumedCostHint),
            ReportComposer.Metric("adjustmentCost", ReportLabels.AdjustmentsSpoilage, stock.AdjustmentCost,
                ReportComposer.Money(stock.AdjustmentCost)),
            ReportComposer.Metric("usedShare", ReportLabels.UsedShareOfPurchases, usedShare, ReportComposer.Percent(usedShare))
        };

        IReadOnlyList<ReportSeries> series =
        [
            new ReportSeries(
                "costMix",
                ReportLabels.CostMix,
                "pie",
                [
                    new ReportSeriesPoint(ReportLabels.Consumed, stock.ConsumeCost),
                    new ReportSeriesPoint(ReportLabels.Adjustments, stock.AdjustmentCost),
                    new ReportSeriesPoint(ReportLabels.RemainingPurchases, Math.Max(0, stock.PurchaseCost - stock.ConsumeCost))
                ])
        ];

        var tables = new List<ReportTable>
        {
            new(
                "foodCostDaily",
                ReportLabels.DailyUsageCost,
                [
                    new ReportColumn("day", ReportLabels.Day),
                    new ReportColumn("purchaseCost", ReportLabels.Purchased, "money"),
                    new ReportColumn("consumeCost", ReportLabels.Consumed, "money"),
                    new ReportColumn("adjustmentCost", ReportLabels.Adjustments, "money")
                ],
                stock.Daily.Select(d => ReportComposer.Row(
                    ("day", d.Day.ToString("yyyy-MM-dd")),
                    ("purchaseCost", d.PurchaseCost),
                    ("consumeCost", d.ConsumeCost),
                    ("adjustmentCost", d.AdjustmentCost))).ToList())
        };

        return new ReportResponse("foodCost", ReportTitles.FoodCost, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetShortageForecastAsync(
        DateOnly? targetDate,
        bool? onlyDeficits,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var day = targetDate ?? _clock.LocalToday;
        var demand = await _orders.GetConfirmedDemandAsync(workspaceId, day, cancellationToken);
        var expected = await ExpandExpectedUsageAsync(workspaceId, demand, cancellationToken);
        var balances = (await _inventory.GetIngredientBalancesAsync(
            workspaceId, expected.Keys, cancellationToken))
            .ToDictionary(r => r.IngredientId);
        var catalog = await _ingredients.GetByIdsAsync(expected.Keys, workspaceId, cancellationToken);

        var rows = expected
            .Select(kv =>
            {
                catalog.TryGetValue(kv.Key, out var info);
                balances.TryGetValue(kv.Key, out var balance);
                var available = balance?.Quantity ?? 0m;
                var toBuy = Math.Max(0m, kv.Value - available);
                return ReportComposer.Row(
                    ("ingredientId", kv.Key),
                    ("name", info?.Name ?? balance?.Name ?? kv.Key.ToString()),
                    ("unit", info?.BaseUnit ?? balance?.Unit),
                    ("requiredQuantity", kv.Value),
                    ("availableQuantity", available),
                    ("toBuyQuantity", toBuy),
                    ("isDeficit", toBuy > 0));
            })
            .OrderByDescending(r => Convert.ToDecimal(r["toBuyQuantity"] ?? 0m))
            .ThenBy(r => Convert.ToString(r["name"]))
            .ToList();

        if (onlyDeficits == true)
        {
            rows = rows.Where(r => r.TryGetValue("isDeficit", out var flag) && flag is true).ToList();
        }

        var deficitCount = rows.Count(r => r.TryGetValue("isDeficit", out var flag) && flag is true);
        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("ingredientCount", ReportLabels.Ingredients, rows.Count, rows.Count.ToString()),
            ReportComposer.Metric("deficitCount", ReportLabels.Shortages, deficitCount, deficitCount.ToString()),
            ReportComposer.Metric("portions", ReportLabels.ConfirmedPortions, demand.Sum(d => d.Portions),
                demand.Sum(d => d.Portions).ToString())
        };

        var series = new List<ReportSeries>
        {
            new(
                "toBuy",
                ReportLabels.ToBuy,
                "bar",
                rows.Where(r => Convert.ToDecimal(r["toBuyQuantity"] ?? 0m) > 0)
                    .Take(12)
                    .Select(r => new ReportSeriesPoint(
                        Convert.ToString(r["name"]) ?? ReportLabels.Ingredient,
                        Convert.ToDecimal(r["toBuyQuantity"] ?? 0m)))
                    .ToList())
        };

        var tables = new List<ReportTable>
        {
            new(
                "shortageForecast",
                ReportLabels.ShoppingShortageForecast,
                [
                    new ReportColumn("name", ReportLabels.Ingredient),
                    new ReportColumn("unit", ReportLabels.Unit),
                    new ReportColumn("requiredQuantity", ReportLabels.Required, "number"),
                    new ReportColumn("availableQuantity", ReportLabels.OnHand, "number"),
                    new ReportColumn("toBuyQuantity", ReportLabels.ToBuy, "number")
                ],
                rows)
        };

        return new ReportResponse("shortageForecast", ReportTitles.ShortageForecast, day, day, metrics, tables, series);
    }

    public async Task<ReportResponse> GetDriverEfficiencyAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? driverId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var rows = await _orders.GetDriverEfficiencyAsync(workspaceId, from, to, driverId, cancellationToken);
        var names = await _users.GetDisplayNamesAsync(
            rows.Where(r => r.DriverId is not null).Select(r => r.DriverId!.Value),
            cancellationToken);

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("driverCount", ReportLabels.Drivers, rows.Count, rows.Count.ToString()),
            ReportComposer.Metric("orderCount", ReportLabels.DeliveredOrders, rows.Sum(r => r.OrderCount),
                rows.Sum(r => r.OrderCount).ToString()),
            ReportComposer.Metric("portions", ReportLabels.Portions, rows.Sum(r => r.Portions),
                rows.Sum(r => r.Portions).ToString()),
            ReportComposer.Metric("revenue", ReportLabels.DeliveredRevenue, rows.Sum(r => r.Revenue),
                ReportComposer.Money(rows.Sum(r => r.Revenue)))
        };

        var tableRows = rows.Select(r =>
        {
            var name = r.DriverId is Guid id && names.TryGetValue(id, out var display)
                ? display
                : ReportLabels.Unassigned;
            return ReportComposer.Row(
                ("driverId", r.DriverId),
                ("driverName", name),
                ("orderCount", r.OrderCount),
                ("portions", r.Portions),
                ("distinctClients", r.DistinctClients),
                ("revenue", r.Revenue));
        }).ToList();

        var series = new List<ReportSeries>
        {
            new(
                "ordersByDriver",
                ReportLabels.DeliveredOrdersByDriver,
                "bar",
                tableRows.Take(12).Select(r => new ReportSeriesPoint(
                    Convert.ToString(r["driverName"]) ?? ReportLabels.Driver,
                    Convert.ToDecimal(r["orderCount"] ?? 0))).ToList())
        };

        var tables = new List<ReportTable>
        {
            new(
                "driverEfficiency",
                ReportLabels.DriverFulfillment,
                [
                    new ReportColumn("driverName", ReportLabels.Driver),
                    new ReportColumn("orderCount", ReportLabels.Orders, "number"),
                    new ReportColumn("portions", ReportLabels.Portions, "number"),
                    new ReportColumn("distinctClients", ReportLabels.Clients, "number"),
                    new ReportColumn("revenue", ReportLabels.Revenue, "money")
                ],
                tableRows)
        };

        return new ReportResponse("driverEfficiency", ReportTitles.DriverEfficiency, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetSupplierSpendAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? supplierId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var rows = await _inventory.GetSupplierSpendAsync(
            workspaceId, from, to, supplierId, cancellationToken, timeFrom, timeTo);
        var total = rows.Sum(r => r.Spend);

        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("spend", ReportLabels.PurchaseSpend, total, ReportComposer.Money(total)),
            ReportComposer.Metric("quantity", ReportLabels.VolumeBought, rows.Sum(r => r.Quantity),
                ReportComposer.Quantity(rows.Sum(r => r.Quantity))),
            ReportComposer.Metric("supplierCount", ReportLabels.Suppliers, rows.Count, rows.Count.ToString()),
            ReportComposer.Metric("batchCount", ReportLabels.Receipts, rows.Sum(r => r.BatchCount),
                rows.Sum(r => r.BatchCount).ToString())
        };

        var tableRows = rows.Select(r =>
        {
            var share = total > 0 ? Math.Round(100m * r.Spend / total, 1) : 0m;
            return ReportComposer.Row(
                ("supplierId", r.SupplierId),
                ("supplierName", r.SupplierName),
                ("spend", r.Spend),
                ("quantity", r.Quantity),
                ("batchCount", r.BatchCount),
                ("sharePercent", share));
        }).ToList();

        var series = new List<ReportSeries>
        {
            new(
                "spendBySupplier",
                ReportLabels.SpendBySupplier,
                "pie",
                rows.Take(8).Select(r => new ReportSeriesPoint(r.SupplierName, r.Spend)).ToList())
        };

        var tables = new List<ReportTable>
        {
            new(
                "supplierSpend",
                ReportLabels.SupplierSpend,
                [
                    new ReportColumn("supplierName", ReportLabels.Supplier),
                    new ReportColumn("spend", ReportLabels.Spend, "money"),
                    new ReportColumn("quantity", ReportLabels.Volume, "number"),
                    new ReportColumn("batchCount", ReportLabels.Receipts, "number"),
                    new ReportColumn("sharePercent", ReportLabels.SharePercent, "number")
                ],
                tableRows)
        };

        return new ReportResponse("supplierSpend", ReportTitles.SupplierSpend, from, to, metrics, tables, series);
    }

    public async Task<ReportResponse> GetCancellationsAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var (from, to) = ResolveRange(dateFrom, dateTo);
        var rows = await _orders.GetCancellationsAsync(workspaceId, from, to, clientCompanyId, cancellationToken);
        var contacts = await _clients.GetContactsAsync(
            workspaceId, rows.Select(r => r.ClientCompanyId), cancellationToken);

        var lost = rows.Sum(r => r.LostRevenue);
        var metrics = new List<ReportMetric>
        {
            ReportComposer.Metric("orderCount", ReportLabels.CancelledOrders, rows.Sum(r => r.OrderCount),
                rows.Sum(r => r.OrderCount).ToString()),
            ReportComposer.Metric("lostRevenue", ReportLabels.LostRevenue, lost, ReportComposer.Money(lost)),
            ReportComposer.Metric("portions", ReportLabels.LostPortions, rows.Sum(r => r.Portions),
                rows.Sum(r => r.Portions).ToString()),
            ReportComposer.Metric("clientCount", ReportLabels.Clients, rows.Select(r => r.ClientCompanyId).Distinct().Count(),
                rows.Select(r => r.ClientCompanyId).Distinct().Count().ToString())
        };

        var tableRows = rows.Select(r =>
        {
            contacts.TryGetValue(r.ClientCompanyId, out var client);
            return ReportComposer.Row(
                ("clientCompanyId", r.ClientCompanyId),
                ("clientName", client?.Name ?? r.ClientCompanyId.ToString()),
                ("targetDate", r.TargetDate.ToString("yyyy-MM-dd")),
                ("reason", r.Reason),
                ("orderCount", r.OrderCount),
                ("portions", r.Portions),
                ("lostRevenue", r.LostRevenue));
        }).ToList();

        var byClient = rows
            .GroupBy(r => r.ClientCompanyId)
            .Select(g =>
            {
                contacts.TryGetValue(g.Key, out var client);
                return new ReportSeriesPoint(client?.Name ?? ReportLabels.Client, g.Sum(x => x.LostRevenue));
            })
            .OrderByDescending(p => p.Value)
            .Take(12)
            .ToList();

        var tables = new List<ReportTable>
        {
            new(
                "cancellations",
                ReportLabels.CancellationsByClientDate,
                [
                    new ReportColumn("targetDate", ReportLabels.Date),
                    new ReportColumn("clientName", ReportLabels.Client),
                    new ReportColumn("reason", ReportLabels.Reason),
                    new ReportColumn("orderCount", ReportLabels.Orders, "number"),
                    new ReportColumn("portions", ReportLabels.Portions, "number"),
                    new ReportColumn("lostRevenue", ReportLabels.LostRevenue, "money")
                ],
                tableRows)
        };

        IReadOnlyList<ReportSeries> series =
        [
            new ReportSeries("lostRevenueByClient", ReportLabels.LostRevenueByClient, "bar", byClient)
        ];

        return new ReportResponse("cancellations", ReportTitles.Cancellations, from, to, metrics, tables, series);
    }

    private async Task<Dictionary<Guid, decimal>> ExpandExpectedUsageAsync(
        Guid workspaceId,
        IReadOnlyList<ProductionDemandLine> demand,
        CancellationToken cancellationToken)
    {
        var expected = new Dictionary<Guid, decimal>();
        if (demand.Count == 0)
        {
            return expected;
        }

        var recipes = await _recipes.GetRecipesByMenuItemIdsAsync(
            workspaceId, demand.Select(d => d.MenuItemId), cancellationToken);

        foreach (var line in demand)
        {
            if (!recipes.TryGetValue(line.MenuItemId, out var recipe))
            {
                continue;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                expected.TryGetValue(ingredient.IngredientId, out var current);
                expected[ingredient.IngredientId] = current + ingredient.QuantityPerPortion * line.Portions;
            }
        }

        return expected;
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new ReportingServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private (DateOnly From, DateOnly To) ResolveRange(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var today = _clock.LocalToday;
        var to = dateTo ?? today;
        var from = dateFrom ?? to.AddDays(-6);
        if (from > to)
        {
            throw new ReportingServiceException("dateFrom must be on or before dateTo.");
        }

        return (from, to);
    }

    private static int CountStatus(TodayOperationsSnapshot snapshot, string status) =>
        snapshot.ByStatus.FirstOrDefault(s => s.Status == status)?.OrderCount ?? 0;
}
