using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Inventory.Services;

public interface IInventoryMovementService
{
    Task<PagedInventoryMovementsResponse> GetMovementsAsync(
        int page,
        int pageSize,
        Guid? ingredientId,
        string? type,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryMovementService : IInventoryMovementService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public InventoryMovementService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<PagedInventoryMovementsResponse> GetMovementsAsync(
        int page,
        int pageSize,
        Guid? ingredientId,
        string? type,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Set<InventoryMovement>()
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId);

        if (ingredientId is Guid id)
        {
            query = query.Where(m => m.IngredientId == id);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<InventoryMovementType>(type, ignoreCase: true, out var parsed))
            {
                throw new ServiceException(
                    $"Invalid movement type '{type}'. Allowed: {string.Join(", ", Enum.GetNames<InventoryMovementType>())}.");
            }

            query = query.Where(m => m.Type == parsed);
        }

        if (from is DateTime fromUtc)
        {
            query = query.Where(m => m.CreatedAt >= DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc));
        }

        if (to is DateTime toUtc)
        {
            query = query.Where(m => m.CreatedAt <= DateTime.SpecifyKind(toUtc, DateTimeKind.Utc));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new InventoryMovementListItemResponse(
                m.Id,
                m.IngredientId,
                m.Ingredient.Name,
                m.Ingredient.BaseUnit.ToString(),
                m.Type.ToString(),
                m.Quantity,
                m.SignedQuantity,
                m.TotalCost,
                m.Source,
                m.Reason,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedInventoryMovementsResponse(items, page, pageSize, totalCount);
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new ServiceException("Workspace context is required.", StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }
}
