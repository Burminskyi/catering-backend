using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Inventory.Services;

public sealed class IngredientCatalog : IIngredientCatalog
{
    private readonly AppDbContext _dbContext;

    public IngredientCatalog(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, IngredientCatalogItem>> GetByIdsAsync(
        IEnumerable<Guid> ingredientIds,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var ids = ingredientIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, IngredientCatalogItem>();
        }

        return await _dbContext.Set<Ingredient>()
            .AsNoTracking()
            .Where(i =>
                ids.Contains(i.Id)
                && (i.WorkspaceId == null || i.WorkspaceId == workspaceId))
            .ToDictionaryAsync(
                i => i.Id,
                i => new IngredientCatalogItem(i.Id, i.Name, i.BaseUnit.ToString()),
                cancellationToken);
    }

    public async Task<bool> AreAccessibleAsync(
        IEnumerable<Guid> ingredientIds,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var ids = ingredientIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return true;
        }

        var found = await _dbContext.Set<Ingredient>()
            .AsNoTracking()
            .CountAsync(
                i =>
                    ids.Contains(i.Id)
                    && (i.WorkspaceId == null || i.WorkspaceId == workspaceId),
                cancellationToken);

        return found == ids.Length;
    }
}
