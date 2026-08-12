namespace CateringSaaS.Shared.Contracts;

public sealed record MenuItemOrderSnapshot(
    Guid MenuItemId,
    Guid WorkspaceId,
    Guid? MenuClientCompanyId,
    DateOnly MenuDayDate,
    decimal SellingPrice,
    string MenuStatus);

public interface IMenuItemOrderCatalog
{
    Task<IReadOnlyDictionary<Guid, MenuItemOrderSnapshot>> GetOrderableSnapshotsAsync(
        IEnumerable<Guid> menuItemIds,
        Guid workspaceId,
        Guid clientCompanyId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
