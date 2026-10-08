namespace CateringSaaS.Shared.Contracts;

public sealed record OrderStatusCount(
    string Status,
    int OrderCount,
    int Portions,
    decimal Revenue);

public sealed record TodayOrderLine(
    Guid OrderId,
    Guid ClientCompanyId,
    Guid? DriverId,
    string Status,
    int Portions,
    decimal TotalAmount,
    DateOnly TargetDate);

public sealed record TodayOperationsSnapshot(
    IReadOnlyList<OrderStatusCount> ByStatus,
    IReadOnlyList<TodayOrderLine> Orders,
    int UnassignedReadyCount,
    int AssignedReadyCount,
    int ReadyPortions,
    int InProductionPortions,
    int WaitingPortions);

public sealed record ClientRevenueRow(
    Guid ClientCompanyId,
    int OrderCount,
    int Portions,
    decimal Revenue);

public sealed record DishPopularityRow(
    string DishName,
    Guid? DishId,
    int Portions,
    int OrderCount,
    decimal Revenue);

public sealed record DeliveryReviewRow(
    Guid ReviewId,
    Guid? MenuItemId,
    int Rating,
    bool IsReclamation,
    string? Comment);

public sealed record DeliveryAuditRow(
    Guid OrderId,
    Guid ClientCompanyId,
    Guid? DriverId,
    DateOnly TargetDate,
    decimal TotalAmount,
    int Portions,
    string Dishes,
    IReadOnlyList<Guid> MenuItemIds,
    IReadOnlyList<DeliveryReviewRow> Reviews);

public interface IOrderReportingQueries
{
    Task<TodayOperationsSnapshot> GetTodayOperationsAsync(
        Guid workspaceId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    Task<TodayOperationsSnapshot> GetOperationsAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientRevenueRow>> GetRevenueByClientAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DishPopularityRow>> GetDishPopularityAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryAuditRow>> GetDeliveryAuditAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReclamationHeatRow>> GetReclamationHeatMapAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        int maxRating,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionDemandLine>> GetProductionDemandAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionDemandLine>> GetConfirmedDemandAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverEfficiencyRow>> GetDriverEfficiencyAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? driverId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CancellationRow>> GetCancellationsAsync(
        Guid workspaceId,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);
}

public sealed record ReclamationHeatRow(
    Guid ClientCompanyId,
    Guid MenuItemId,
    string DishName,
    int ReviewCount,
    decimal AverageRating,
    int MinRating);

public sealed record ProductionDemandLine(
    Guid MenuItemId,
    Guid? DishId,
    string DishName,
    int Portions);

public sealed record DriverEfficiencyRow(
    Guid? DriverId,
    int OrderCount,
    int Portions,
    int DistinctClients,
    decimal Revenue);

public sealed record CancellationRow(
    Guid ClientCompanyId,
    DateOnly TargetDate,
    string Reason,
    int OrderCount,
    int Portions,
    decimal LostRevenue);
