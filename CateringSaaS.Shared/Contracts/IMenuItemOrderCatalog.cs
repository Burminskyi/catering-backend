namespace CateringSaaS.Shared.Contracts;

public sealed record MenuItemOrderSnapshot(
    Guid MenuItemId,
    Guid WorkspaceId,
    Guid? MenuClientCompanyId,
    DateOnly MenuDayDate,
    decimal SellingPrice,
    string MenuStatus,
    Guid DishId,
    string DishName);

public sealed record MenuItemDisplaySnapshot(
    Guid MenuItemId,
    Guid DishId,
    string DishName);

public interface IMenuItemOrderCatalog
{
    Task<IReadOnlyDictionary<Guid, MenuItemOrderSnapshot>> GetOrderableSnapshotsAsync(
        IEnumerable<Guid> menuItemIds,
        Guid workspaceId,
        Guid clientCompanyId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves dish display info for menu items (including historical / unpublished).
    /// Used to backfill names for meal-request lines that predate DishName snapshots.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, MenuItemDisplaySnapshot>> GetDisplaySnapshotsAsync(
        IEnumerable<Guid> menuItemIds,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
