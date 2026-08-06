using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Services;

public interface IIngredientService
{
    Task<PagedIngredientsResponse> GetIngredientsAsync(
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken = default);

    Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class IngredientService : IIngredientService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public IngredientService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<PagedIngredientsResponse> GetIngredientsAsync(
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Set<Ingredient>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == null || i.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(i => i.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category)
            && Enum.TryParse<IngredientCategory>(category, ignoreCase: true, out var categoryFilter))
        {
            query = query.Where(i => i.Category == categoryFilter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new IngredientResponse(
                i.Id,
                i.Name,
                i.Category.ToString(),
                i.BaseUnit.ToString(),
                i.WorkspaceId,
                i.WorkspaceId == null))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedIngredientsResponse(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<IngredientResponse> CreateAsync(
        CreateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var name = request.Name.Trim();
        var category = ParseCategory(request.Category);
        var baseUnit = ParseBaseUnit(request.BaseUnit);

        await EnsureNameUniqueAsync(name, workspaceId, excludeId: null, cancellationToken);

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            BaseUnit = baseUnit,
            WorkspaceId = workspaceId
        };

        await _dbContext.Set<Ingredient>().AddAsync(ingredient, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(ingredient);
    }

    public async Task<IngredientResponse> UpdateAsync(
        Guid id,
        UpdateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var ingredient = await GetLocalIngredientOrThrowAsync(id, workspaceId, cancellationToken);

        var name = request.Name.Trim();
        var category = ParseCategory(request.Category);
        var baseUnit = ParseBaseUnit(request.BaseUnit);

        await EnsureNameUniqueAsync(name, workspaceId, excludeId: id, cancellationToken);

        ingredient.Name = name;
        ingredient.Category = category;
        ingredient.BaseUnit = baseUnit;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(ingredient);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var ingredient = await GetLocalIngredientOrThrowAsync(id, workspaceId, cancellationToken);

        var hasActiveBatches = await _dbContext.Set<StockBatch>()
            .AnyAsync(
                b => b.IngredientId == id && b.CurrentQuantity > 0,
                cancellationToken);

        if (hasActiveBatches)
        {
            throw new ServiceException(
                "Cannot delete ingredient while active stock batches remain. Consume or adjust stock first.",
                StatusCodes.Status409Conflict);
        }

        var hasInventory = await _dbContext.Set<InventoryEntity>()
            .AnyAsync(
                i => i.IngredientId == id && i.TotalQuantity > 0,
                cancellationToken);

        if (hasInventory)
        {
            throw new ServiceException(
                "Cannot delete ingredient while inventory balance is greater than zero.",
                StatusCodes.Status409Conflict);
        }

        var inventoryRows = await _dbContext.Set<InventoryEntity>()
            .Where(i => i.IngredientId == id)
            .ToListAsync(cancellationToken);
        _dbContext.Set<InventoryEntity>().RemoveRange(inventoryRows);

        var batches = await _dbContext.Set<StockBatch>()
            .Where(b => b.IngredientId == id)
            .ToListAsync(cancellationToken);
        _dbContext.Set<StockBatch>().RemoveRange(batches);

        _dbContext.Set<Ingredient>().Remove(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ingredient> GetLocalIngredientOrThrowAsync(
        Guid id,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var ingredient = await _dbContext.Set<Ingredient>()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (ingredient is null)
        {
            throw new ServiceException($"Ingredient '{id}' was not found.", StatusCodes.Status404NotFound);
        }

        if (ingredient.WorkspaceId is null)
        {
            throw new ServiceException(
                "Global system ingredients cannot be modified or deleted by tenants.",
                StatusCodes.Status403Forbidden);
        }

        if (ingredient.WorkspaceId != workspaceId)
        {
            throw new ServiceException(
                "You can only modify ingredients that belong to your workspace.",
                StatusCodes.Status403Forbidden);
        }

        return ingredient;
    }

    private async Task EnsureNameUniqueAsync(
        string name,
        Guid workspaceId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        var query = _dbContext.Set<Ingredient>()
            .Where(i => i.Name.ToLower() == normalized
                        && (i.WorkspaceId == null || i.WorkspaceId == workspaceId));

        if (excludeId is Guid id)
        {
            query = query.Where(i => i.Id != id);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ServiceException(
                "An ingredient with this name already exists in the global catalog or in the current workspace.",
                StatusCodes.Status409Conflict);
        }
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new ServiceException("Workspace context is required.", StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private static IngredientCategory ParseCategory(string value)
    {
        if (!Enum.TryParse<IngredientCategory>(value, ignoreCase: true, out var category))
        {
            throw new ServiceException($"Invalid Category '{value}'.");
        }

        return category;
    }

    private static UnitOfMeasure ParseBaseUnit(string value)
    {
        if (!Enum.TryParse<UnitOfMeasure>(value, ignoreCase: true, out var unit))
        {
            throw new ServiceException($"Invalid BaseUnit '{value}'.");
        }

        return unit;
    }

    private static IngredientResponse ToResponse(Ingredient ingredient) =>
        new(
            ingredient.Id,
            ingredient.Name,
            ingredient.Category.ToString(),
            ingredient.BaseUnit.ToString(),
            ingredient.WorkspaceId,
            ingredient.WorkspaceId is null);
}
