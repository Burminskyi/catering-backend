using CateringSaaS.Modules.Menu.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Menu.Services;

public sealed class DishRecipeCatalog : IDishRecipeCatalog
{
    private readonly AppDbContext _dbContext;

    public DishRecipeCatalog(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, MenuItemRecipeSnapshot>> GetRecipesByMenuItemIdsAsync(
        Guid workspaceId,
        IEnumerable<Guid> menuItemIds,
        CancellationToken cancellationToken = default)
    {
        var ids = menuItemIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, MenuItemRecipeSnapshot>();
        }

        var rows = await _dbContext.Set<MenuItem>()
            .AsNoTracking()
            .Where(i => ids.Contains(i.Id) && i.WorkspaceId == workspaceId)
            .Select(i => new
            {
                i.Id,
                i.DishId,
                DishName = i.Dish.Name,
                Ingredients = i.Dish.Ingredients
                    .Select(di => new DishRecipeIngredientLine(di.IngredientId, di.Quantity))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Id,
            r => new MenuItemRecipeSnapshot(
                r.Id,
                r.DishId,
                r.DishName,
                r.Ingredients));
    }
}
