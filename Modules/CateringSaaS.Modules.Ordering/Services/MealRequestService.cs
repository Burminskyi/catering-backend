using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public interface IEmployeeMealRequestService
{
    Task<MealRequestResponse> CreateAsync(
        CreateMealRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealRequestListItemResponse>> GetMyAsync(
        CancellationToken cancellationToken = default);
}

public interface IClientAdminMealRequestService
{
    Task<IReadOnlyList<MealRequestListItemResponse>> GetSubmittedForDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);

    Task<ConsolidateMealRequestsResponse> ConsolidateAsync(
        ConsolidateMealRequestsRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class EmployeeMealRequestService : IEmployeeMealRequestService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMenuItemOrderCatalog _menuItemCatalog;
    private readonly IUserDisplayLookup _userDisplayLookup;

    public EmployeeMealRequestService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IMenuItemOrderCatalog menuItemCatalog,
        IUserDisplayLookup userDisplayLookup)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _menuItemCatalog = menuItemCatalog;
        _userDisplayLookup = userDisplayLookup;
    }

    public async Task<MealRequestResponse> CreateAsync(
        CreateMealRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureClientEmployee();
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var employeeId = RequireUserId();

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new OrderServiceException("At least one meal request item is required.");
        }

        if (request.TargetDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new OrderServiceException("TargetDate must be today or a future date.");
        }

        var normalizedItems = NormalizeItems(request.Items);

        var hasActive = await _dbContext.Set<EmployeeMealRequest>()
            .AnyAsync(
                r => r.WorkspaceId == workspaceId
                    && r.EmployeeId == employeeId
                    && r.TargetDate == request.TargetDate
                    && (r.Status == EmployeeMealRequestStatus.Submitted
                        || r.Status == EmployeeMealRequestStatus.Approved),
                cancellationToken);

        if (hasActive)
        {
            throw new OrderServiceException(
                $"An active meal request already exists for {request.TargetDate:yyyy-MM-dd}.",
                StatusCodes.Status409Conflict);
        }

        var snapshots = await _menuItemCatalog.GetOrderableSnapshotsAsync(
            normalizedItems.Select(i => i.MenuItemId),
            workspaceId,
            clientCompanyId,
            request.TargetDate,
            cancellationToken);

        if (snapshots.Count != normalizedItems.Count)
        {
            throw new OrderServiceException(
                "One or more menu items are not available for the selected date.",
                StatusCodes.Status400BadRequest);
        }

        var items = normalizedItems.Select(input =>
        {
            var snapshot = snapshots[input.MenuItemId];
            var subtotal = snapshot.SellingPrice * input.Quantity;
            return new EmployeeMealRequestItem
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MenuItemId = input.MenuItemId,
                DishId = snapshot.DishId,
                DishName = snapshot.DishName,
                Quantity = input.Quantity,
                UnitPrice = snapshot.SellingPrice,
                Subtotal = subtotal
            };
        }).ToList();

        var mealRequest = new EmployeeMealRequest
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            EmployeeId = employeeId,
            TargetDate = request.TargetDate,
            Status = EmployeeMealRequestStatus.Submitted,
            TotalAmount = items.Sum(i => i.Subtotal),
            CreatedAt = DateTime.UtcNow,
            Items = items
        };

        await _dbContext.Set<EmployeeMealRequest>().AddAsync(mealRequest, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(mealRequest);
    }

    public async Task<IReadOnlyList<MealRequestListItemResponse>> GetMyAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureClientEmployee();
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var employeeId = RequireUserId();

        var requests = await _dbContext.Set<EmployeeMealRequest>()
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.WorkspaceId == workspaceId
                && r.ClientCompanyId == clientCompanyId
                && r.EmployeeId == employeeId)
            .OrderByDescending(r => r.TargetDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MealRequestListMapper.MapAsync(
            requests,
            workspaceId,
            _menuItemCatalog,
            _userDisplayLookup,
            cancellationToken);
    }

    private void EnsureClientEmployee()
    {
        if (!string.Equals(_currentUser.Role, "ClientEmployee", StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderServiceException(
                "Only ClientEmployee can manage personal meal requests.",
                StatusCodes.Status403Forbidden);
        }
    }

    private static IReadOnlyList<CreateMealRequestItemInput> NormalizeItems(
        IReadOnlyList<CreateMealRequestItemInput> items)
    {
        var grouped = items
            .GroupBy(i => i.MenuItemId)
            .Select(g => new CreateMealRequestItemInput(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        if (grouped.Any(i => i.MenuItemId == Guid.Empty || i.Quantity <= 0))
        {
            throw new OrderServiceException("Each item must have a valid MenuItemId and Quantity > 0.");
        }

        return grouped;
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private Guid RequireClientCompany()
    {
        if (_currentUser.ClientCompanyId is not Guid clientCompanyId || clientCompanyId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Client company context is required.",
                StatusCodes.Status400BadRequest);
        }

        return clientCompanyId;
    }

    private Guid RequireUserId()
    {
        if (_currentUser.UserId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Authenticated user is required.",
                StatusCodes.Status401Unauthorized);
        }

        return _currentUser.UserId;
    }

    private static MealRequestResponse ToResponse(EmployeeMealRequest request) =>
        new(
            request.Id,
            request.WorkspaceId,
            request.ClientCompanyId,
            request.EmployeeId,
            request.TargetDate,
            request.Status.ToString(),
            request.TotalAmount,
            request.CreatedAt,
            request.Items.Select(i => new MealRequestItemResponse(
                i.Id,
                i.MenuItemId,
                i.Quantity,
                i.UnitPrice,
                i.Subtotal)).ToList());
}

public sealed class ClientAdminMealRequestService : IClientAdminMealRequestService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMenuItemOrderCatalog _menuItemCatalog;
    private readonly IUserDisplayLookup _userDisplayLookup;

    public ClientAdminMealRequestService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IMenuItemOrderCatalog menuItemCatalog,
        IUserDisplayLookup userDisplayLookup)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _menuItemCatalog = menuItemCatalog;
        _userDisplayLookup = userDisplayLookup;
    }

    public async Task<IReadOnlyList<MealRequestListItemResponse>> GetSubmittedForDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        EnsureClientAdmin();
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();

        var requests = await _dbContext.Set<EmployeeMealRequest>()
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.WorkspaceId == workspaceId
                && r.ClientCompanyId == clientCompanyId
                && r.TargetDate == targetDate
                && r.Status == EmployeeMealRequestStatus.Submitted)
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MealRequestListMapper.MapAsync(
            requests,
            workspaceId,
            _menuItemCatalog,
            _userDisplayLookup,
            cancellationToken);
    }

    public async Task<ConsolidateMealRequestsResponse> ConsolidateAsync(
        ConsolidateMealRequestsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureClientAdmin();
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var adminUserId = RequireUserId();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var submitted = await _dbContext.Set<EmployeeMealRequest>()
            .Include(r => r.Items)
            .Where(r => r.WorkspaceId == workspaceId
                && r.ClientCompanyId == clientCompanyId
                && r.TargetDate == request.TargetDate
                && r.Status == EmployeeMealRequestStatus.Submitted)
            .ToListAsync(cancellationToken);

        if (submitted.Count == 0)
        {
            throw new OrderServiceException(
                $"No submitted meal requests found for {request.TargetDate:yyyy-MM-dd}.",
                StatusCodes.Status404NotFound);
        }

        var aggregated = submitted
            .SelectMany(r => r.Items)
            .GroupBy(i => i.MenuItemId)
            .Select(g =>
            {
                var first = g.First();
                var unitPrice = first.UnitPrice;
                var quantity = g.Sum(x => x.Quantity);
                return new OrderItem
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    MenuItemId = g.Key,
                    DishId = first.DishId,
                    DishName = first.DishName,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    Subtotal = unitPrice * quantity
                };
            })
            .ToList();

        // Backfill dish snapshots for legacy meal-request lines without DishName.
        var missingMenuItemIds = aggregated
            .Where(i => string.IsNullOrWhiteSpace(i.DishName) || i.DishId is null)
            .Select(i => i.MenuItemId)
            .Distinct()
            .ToArray();

        if (missingMenuItemIds.Length > 0)
        {
            var displays = await _menuItemCatalog.GetDisplaySnapshotsAsync(
                missingMenuItemIds,
                workspaceId,
                cancellationToken);

            foreach (var item in aggregated)
            {
                if (!displays.TryGetValue(item.MenuItemId, out var display))
                {
                    continue;
                }

                item.DishId ??= display.DishId;
                if (string.IsNullOrWhiteSpace(item.DishName))
                {
                    item.DishName = display.DishName;
                }
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            PlacedByUserId = adminUserId,
            TargetDate = request.TargetDate,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = aggregated.Sum(i => i.Subtotal),
            Items = aggregated
        };

        await _dbContext.Set<Order>().AddAsync(order, cancellationToken);

        foreach (var mealRequest in submitted)
        {
            mealRequest.Status = EmployeeMealRequestStatus.Approved;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ConsolidateMealRequestsResponse(
            order.Id,
            order.TargetDate,
            submitted.Count,
            order.TotalAmount,
            order.Status.ToString());
    }

    private void EnsureClientAdmin()
    {
        if (!string.Equals(_currentUser.Role, "ClientAdmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderServiceException(
                "Only ClientAdmin can consolidate meal requests.",
                StatusCodes.Status403Forbidden);
        }
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private Guid RequireClientCompany()
    {
        if (_currentUser.ClientCompanyId is not Guid clientCompanyId || clientCompanyId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Client company context is required.",
                StatusCodes.Status400BadRequest);
        }

        return clientCompanyId;
    }

    private Guid RequireUserId()
    {
        if (_currentUser.UserId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Authenticated user is required.",
                StatusCodes.Status401Unauthorized);
        }

        return _currentUser.UserId;
    }
}

internal static class MealRequestListMapper
{
    public static async Task<IReadOnlyList<MealRequestListItemResponse>> MapAsync(
        IReadOnlyList<EmployeeMealRequest> requests,
        Guid workspaceId,
        IMenuItemOrderCatalog menuItemCatalog,
        IUserDisplayLookup userDisplayLookup,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return Array.Empty<MealRequestListItemResponse>();
        }

        var employeeIds = requests.Select(r => r.EmployeeId).Distinct();
        var names = await userDisplayLookup.GetDisplayNamesAsync(employeeIds, cancellationToken);

        var missingNameMenuItemIds = requests
            .SelectMany(r => r.Items)
            .Where(i => string.IsNullOrWhiteSpace(i.DishName) || i.DishId is null)
            .Select(i => i.MenuItemId)
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<Guid, MenuItemDisplaySnapshot> displays =
            missingNameMenuItemIds.Length == 0
                ? new Dictionary<Guid, MenuItemDisplaySnapshot>()
                : await menuItemCatalog.GetDisplaySnapshotsAsync(
                    missingNameMenuItemIds,
                    workspaceId,
                    cancellationToken);

        return requests.Select(r =>
        {
            var lines = r.Items
                .OrderBy(i => i.DishName)
                .ThenBy(i => i.MenuItemId)
                .Select(i =>
                {
                    displays.TryGetValue(i.MenuItemId, out var display);
                    var dishName = !string.IsNullOrWhiteSpace(i.DishName)
                        ? i.DishName
                        : display?.DishName ?? string.Empty;
                    var dishId = i.DishId ?? display?.DishId;

                    return new MealRequestListLineResponse(
                        i.Id,
                        i.MenuItemId,
                        dishId,
                        dishName,
                        i.Quantity,
                        i.UnitPrice,
                        i.Subtotal);
                })
                .ToList();

            names.TryGetValue(r.EmployeeId, out var employeeName);

            return new MealRequestListItemResponse(
                r.Id,
                r.EmployeeId,
                employeeName,
                r.TargetDate,
                r.Status.ToString(),
                r.TotalAmount,
                r.CreatedAt,
                lines.Count,
                lines);
        }).ToList();
    }
}
