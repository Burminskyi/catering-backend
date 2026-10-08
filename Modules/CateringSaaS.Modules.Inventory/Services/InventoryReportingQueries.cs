using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Services;

public sealed class InventoryReportingQueries : IInventoryReportingQueries
{
    private readonly AppDbContext _dbContext;
    private readonly IClientTimeContext _clock;

    public InventoryReportingQueries(AppDbContext dbContext, IClientTimeContext clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CriticalStockRow>> GetCriticalStockAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        // Threshold filter in SQL (matches StockThreshold.ForUnitEnum).
        var rows = await _dbContext.Set<InventoryEntity>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId)
            .Where(i =>
                i.TotalQuantity < (
                    i.Ingredient.BaseUnit == UnitOfMeasure.Milliliter ? 10_000m
                    : i.Ingredient.BaseUnit == UnitOfMeasure.Piece ? 20m
                    : 6_000m))
            .OrderBy(i => i.TotalQuantity)
            .ThenBy(i => i.Ingredient.Name)
            .Take(20)
            .Select(i => new
            {
                i.IngredientId,
                Name = i.Ingredient.Name,
                Category = i.Ingredient.Category,
                Unit = i.Ingredient.BaseUnit,
                i.TotalQuantity
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CriticalStockRow(
                row.IngredientId,
                row.Name,
                row.Category.ToString(),
                row.Unit.ToString(),
                row.TotalQuantity,
                ThresholdFor(row.Unit)))
            .ToList();
    }

    public async Task<StockMovementSnapshot> GetStockMovementsAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null,
        DateTime? instantFromUtc = null,
        DateTime? instantToUtcExclusive = null)
    {
        var (fromUtc, toUtcExclusive) = UtcBounds(dateFrom, dateTo, timeFrom, timeTo, instantFromUtc, instantToUtcExclusive);

        var query = _dbContext.Set<InventoryMovement>()
            .AsNoTracking()
            .Where(m =>
                m.WorkspaceId == workspaceId
                && m.CreatedAt >= fromUtc
                && m.CreatedAt < toUtcExclusive);

        if (ingredientId is Guid id)
        {
            query = query.Where(m => m.IngredientId == id);
        }

        // One slim projection: daily buckets need client TZ, so avoid triple table scans.
        var movements = await query
            .Select(m => new
            {
                m.Type,
                m.Quantity,
                m.TotalCost,
                m.CreatedAt,
                Category = m.Ingredient.Category
            })
            .ToListAsync(cancellationToken);

        decimal Qty(InventoryMovementType type) =>
            movements.Where(m => m.Type == type).Sum(m => m.Quantity);
        decimal Cost(InventoryMovementType type) =>
            movements.Where(m => m.Type == type).Sum(m => m.TotalCost);

        var daily = movements
            .GroupBy(m => LocalDate(m.CreatedAt))
            .OrderBy(g => g.Key)
            .Select(g => new StockMovementDayRow(
                g.Key,
                g.Where(m => m.Type == InventoryMovementType.Purchase).Sum(m => m.Quantity),
                g.Where(m => m.Type == InventoryMovementType.Purchase).Sum(m => m.TotalCost),
                g.Where(m => m.Type == InventoryMovementType.Consume).Sum(m => m.Quantity),
                g.Where(m => m.Type == InventoryMovementType.Consume).Sum(m => m.TotalCost),
                g.Where(m => m.Type == InventoryMovementType.Adjustment).Sum(m => m.Quantity),
                g.Where(m => m.Type == InventoryMovementType.Adjustment).Sum(m => m.TotalCost)))
            .ToList();

        var categorySpend = movements
            .Where(m => m.Type == InventoryMovementType.Purchase)
            .GroupBy(m => m.Category)
            .Select(g => new StockCategorySpendRow(g.Key.ToString(), g.Sum(m => m.TotalCost)))
            .OrderByDescending(r => r.PurchaseCost)
            .ToList();

        return new StockMovementSnapshot(
            Qty(InventoryMovementType.Purchase),
            Cost(InventoryMovementType.Purchase),
            Qty(InventoryMovementType.Consume),
            Cost(InventoryMovementType.Consume),
            Qty(InventoryMovementType.Adjustment),
            Cost(InventoryMovementType.Adjustment),
            daily,
            categorySpend);
    }

    public async Task<IReadOnlyList<IngredientConsumptionRow>> GetConsumptionByIngredientAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? ingredientId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null,
        DateTime? instantFromUtc = null,
        DateTime? instantToUtcExclusive = null)
    {
        var (fromUtc, toUtcExclusive) = UtcBounds(dateFrom, dateTo, timeFrom, timeTo, instantFromUtc, instantToUtcExclusive);
        var query = _dbContext.Set<InventoryMovement>()
            .AsNoTracking()
            .Where(m =>
                m.WorkspaceId == workspaceId
                && m.Type == InventoryMovementType.Consume
                && m.CreatedAt >= fromUtc
                && m.CreatedAt < toUtcExclusive);

        if (ingredientId is Guid id)
        {
            query = query.Where(m => m.IngredientId == id);
        }

        var groups = await query
            .GroupBy(m => new { m.IngredientId, m.Ingredient.Name, Unit = m.Ingredient.BaseUnit })
            .Select(g => new
            {
                g.Key.IngredientId,
                g.Key.Name,
                g.Key.Unit,
                ConsumeQuantity = g.Sum(m => m.Quantity),
                ConsumeCost = g.Sum(m => m.TotalCost)
            })
            .OrderByDescending(g => g.ConsumeQuantity)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return groups
            .Select(g => new IngredientConsumptionRow(
                g.IngredientId,
                g.Name,
                g.Unit.ToString(),
                g.ConsumeQuantity,
                g.ConsumeCost))
            .ToList();
    }

    public async Task<IReadOnlyList<IngredientBalanceRow>> GetIngredientBalancesAsync(
        Guid workspaceId,
        IEnumerable<Guid>? ingredientIds,
        CancellationToken cancellationToken = default)
    {
        var ids = ingredientIds?.Distinct().ToArray() ?? [];
        var query = _dbContext.Set<InventoryEntity>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId);

        if (ids.Length > 0)
        {
            query = query.Where(i => ids.Contains(i.IngredientId));
        }

        // Enum.ToString() is not EF-translatable — project then map.
        var rows = await query
            .Select(i => new
            {
                i.IngredientId,
                Name = i.Ingredient.Name,
                Unit = i.Ingredient.BaseUnit,
                Category = i.Ingredient.Category,
                i.TotalQuantity
            })
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new IngredientBalanceRow(
                r.IngredientId,
                r.Name,
                r.Unit.ToString(),
                r.Category.ToString(),
                r.TotalQuantity))
            .ToList();
    }

    public async Task<IReadOnlyList<SupplierSpendRow>> GetSupplierSpendAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? supplierId,
        CancellationToken cancellationToken = default,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null,
        DateTime? instantFromUtc = null,
        DateTime? instantToUtcExclusive = null)
    {
        var (fromUtc, toUtcExclusive) = UtcBounds(dateFrom, dateTo, timeFrom, timeTo, instantFromUtc, instantToUtcExclusive);
        var query = _dbContext.Set<StockBatch>()
            .AsNoTracking()
            .Where(b =>
                b.WorkspaceId == workspaceId
                && b.ReceivedAt >= fromUtc
                && b.ReceivedAt < toUtcExclusive);

        if (supplierId is Guid id)
        {
            query = query.Where(b => b.SupplierId == id);
        }

        return await query
            .GroupBy(b => new { b.SupplierId, b.Supplier.Name })
            .Select(g => new SupplierSpendRow(
                g.Key.SupplierId,
                g.Key.Name,
                g.Sum(b => b.CostPrice),
                g.Sum(b => b.InitialQuantity),
                g.Count()))
            .OrderByDescending(r => r.Spend)
            .ThenBy(r => r.SupplierName)
            .ToListAsync(cancellationToken);
    }

    private (DateTime FromUtc, DateTime ToUtcExclusive) UtcBounds(
        DateOnly dateFrom,
        DateOnly dateTo,
        TimeOnly? timeFrom,
        TimeOnly? timeTo,
        DateTime? instantFromUtc = null,
        DateTime? instantToUtcExclusive = null)
    {
        if (instantFromUtc is DateTime instantFrom && instantToUtcExclusive is DateTime instantTo && instantTo > instantFrom)
        {
            return (DateTime.SpecifyKind(instantFrom, DateTimeKind.Utc), DateTime.SpecifyKind(instantTo, DateTimeKind.Utc));
        }

        var fromUtc = _clock.ToUtc(dateFrom, timeFrom ?? TimeOnly.MinValue);
        var toUtcExclusive = timeTo is TimeOnly end
            ? _clock.ToUtc(dateTo, end)
            : _clock.ToUtc(dateTo.AddDays(1), TimeOnly.MinValue);

        if (toUtcExclusive <= fromUtc)
        {
            toUtcExclusive = fromUtc.AddMinutes(1);
        }

        return (fromUtc, toUtcExclusive);
    }

    private DateOnly LocalDate(DateTime utc)
    {
        var specified = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(specified, _clock.TimeZone);
        return DateOnly.FromDateTime(local);
    }

    private static decimal ThresholdFor(UnitOfMeasure unit) =>
        CateringSaaS.Shared.Notifications.StockThreshold.ForUnitEnum((int)unit);
}
