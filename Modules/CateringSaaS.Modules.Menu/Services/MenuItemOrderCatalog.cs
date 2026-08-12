using CateringSaaS.Modules.Menu.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using MenuEntity = CateringSaaS.Modules.Menu.Domain.Menu;

namespace CateringSaaS.Modules.Menu.Services;

public sealed class MenuItemOrderCatalog : IMenuItemOrderCatalog
{
    private readonly AppDbContext _dbContext;

    public MenuItemOrderCatalog(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, MenuItemOrderSnapshot>> GetOrderableSnapshotsAsync(
        IEnumerable<Guid> menuItemIds,
        Guid workspaceId,
        Guid clientCompanyId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var ids = menuItemIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, MenuItemOrderSnapshot>();
        }

        var rows = await _dbContext.Set<MenuItem>()
            .AsNoTracking()
            .Where(i =>
                ids.Contains(i.Id)
                && i.WorkspaceId == workspaceId
                && i.MenuDay.Date == targetDate
                && i.MenuDay.Menu.Status == MenuStatus.Published
                && (i.MenuDay.Menu.ClientCompanyId == null
                    || i.MenuDay.Menu.ClientCompanyId == clientCompanyId)
                && targetDate >= i.MenuDay.Menu.StartDate
                && targetDate <= i.MenuDay.Menu.EndDate)
            .Select(i => new MenuItemOrderSnapshot(
                i.Id,
                i.WorkspaceId,
                i.MenuDay.Menu.ClientCompanyId,
                i.MenuDay.Date,
                i.SellingPrice,
                i.MenuDay.Menu.Status.ToString()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.MenuItemId);
    }
}
