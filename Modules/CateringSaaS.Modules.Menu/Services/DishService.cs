using CateringSaaS.Modules.Menu.Domain;
using CateringSaaS.Modules.Menu.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Menu.Services;

public interface IDishService
{
    Task<IReadOnlyList<DishResponse>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<DishResponse> CreateAsync(CreateDishRequest request, CancellationToken cancellationToken = default);

    Task<DishResponse> UpdateAsync(Guid id, UpdateDishRequest request, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class DishService : IDishService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IIngredientCatalog _ingredientCatalog;

    public DishService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        IIngredientCatalog ingredientCatalog)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _ingredientCatalog = ingredientCatalog;
    }

    public async Task<IReadOnlyList<DishResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var dishes = await _dbContext.Set<Dish>()
            .AsNoTracking()
            .Include(d => d.Ingredients)
            .Where(d => d.WorkspaceId == workspaceId && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return await MapDishesAsync(dishes, workspaceId, cancellationToken);
    }

    public async Task<DishResponse> CreateAsync(
        CreateDishRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var category = ParseCategory(request.Category);
        ValidateDishFields(request.Name, request.OutputWeight, request.Ingredients);

        var ingredients = NormalizeIngredients(request.Ingredients);
        await EnsureIngredientsAccessibleAsync(ingredients, workspaceId, cancellationToken);

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Category = category,
            OutputWeight = request.OutputWeight,
            Instructions = NormalizeOptional(request.Instructions),
            IsActive = true,
            Ingredients = ingredients.Select(i => new DishIngredient
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                IngredientId = i.IngredientId,
                Quantity = i.Quantity
            }).ToList()
        };

        await _dbContext.Set<Dish>().AddAsync(dish, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await MapDishesAsync([dish], workspaceId, cancellationToken))[0];
    }

    public async Task<DishResponse> UpdateAsync(
        Guid id,
        UpdateDishRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var category = ParseCategory(request.Category);
        ValidateDishFields(request.Name, request.OutputWeight, request.Ingredients);

        var dish = await _dbContext.Set<Dish>()
            .Include(d => d.Ingredients)
            .FirstOrDefaultAsync(d => d.Id == id && d.WorkspaceId == workspaceId, cancellationToken);

        if (dish is null || !dish.IsActive)
        {
            throw new MenuServiceException($"Dish '{id}' was not found.", StatusCodes.Status404NotFound);
        }

        var ingredients = NormalizeIngredients(request.Ingredients);
        await EnsureIngredientsAccessibleAsync(ingredients, workspaceId, cancellationToken);

        dish.Name = request.Name.Trim();
        dish.Description = NormalizeOptional(request.Description);
        dish.Category = category;
        dish.OutputWeight = request.OutputWeight;
        dish.Instructions = NormalizeOptional(request.Instructions);

        _dbContext.Set<DishIngredient>().RemoveRange(dish.Ingredients);
        dish.Ingredients = ingredients.Select(i => new DishIngredient
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            DishId = dish.Id,
            IngredientId = i.IngredientId,
            Quantity = i.Quantity
        }).ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await MapDishesAsync([dish], workspaceId, cancellationToken))[0];
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var dish = await _dbContext.Set<Dish>()
            .FirstOrDefaultAsync(d => d.Id == id && d.WorkspaceId == workspaceId, cancellationToken);

        if (dish is null || !dish.IsActive)
        {
            throw new MenuServiceException($"Dish '{id}' was not found.", StatusCodes.Status404NotFound);
        }

        dish.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureIngredientsAccessibleAsync(
        IReadOnlyList<DishIngredientInput> ingredients,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var ok = await _ingredientCatalog.AreAccessibleAsync(
            ingredients.Select(i => i.IngredientId),
            workspaceId,
            cancellationToken);

        if (!ok)
        {
            throw new MenuServiceException(
                "One or more ingredients were not found in this workspace catalog.",
                StatusCodes.Status400BadRequest);
        }
    }

    private async Task<IReadOnlyList<DishResponse>> MapDishesAsync(
        IReadOnlyList<Dish> dishes,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var ingredientIds = dishes.SelectMany(d => d.Ingredients).Select(i => i.IngredientId);
        var catalog = await _ingredientCatalog.GetByIdsAsync(ingredientIds, workspaceId, cancellationToken);

        return dishes.Select(d => new DishResponse(
            d.Id,
            d.WorkspaceId,
            d.Name,
            d.Description,
            d.Category.ToString(),
            d.OutputWeight,
            d.Instructions,
            d.IsActive,
            d.Ingredients
                .OrderBy(i => i.IngredientId)
                .Select(i =>
                {
                    catalog.TryGetValue(i.IngredientId, out var info);
                    return new DishIngredientResponse(
                        i.IngredientId,
                        info?.Name ?? "Unknown",
                        info?.BaseUnit ?? "Unknown",
                        i.Quantity);
                })
                .ToList())).ToList();
    }

    private static void ValidateDishFields(
        string name,
        int outputWeight,
        IReadOnlyList<DishIngredientInput>? ingredients)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new MenuServiceException("Name is required.");
        }

        if (outputWeight <= 0)
        {
            throw new MenuServiceException("OutputWeight must be greater than zero.");
        }

        if (ingredients is null || ingredients.Count == 0)
        {
            throw new MenuServiceException("At least one ingredient is required.");
        }
    }

    private static IReadOnlyList<DishIngredientInput> NormalizeIngredients(
        IReadOnlyList<DishIngredientInput> ingredients)
    {
        var normalized = ingredients
            .GroupBy(i => i.IngredientId)
            .Select(g => new DishIngredientInput(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        if (normalized.Any(i => i.IngredientId == Guid.Empty || i.Quantity <= 0))
        {
            throw new MenuServiceException("Each ingredient must have a valid IngredientId and Quantity > 0.");
        }

        return normalized;
    }

    private static DishCategory ParseCategory(string category)
    {
        if (!Enum.TryParse<DishCategory>(category, ignoreCase: true, out var parsed))
        {
            throw new MenuServiceException(
                $"Invalid category '{category}'. Allowed: {string.Join(", ", Enum.GetNames<DishCategory>())}.");
        }

        return parsed;
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new MenuServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
