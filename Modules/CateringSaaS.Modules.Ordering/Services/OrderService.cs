using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public interface IClientOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItemResponse>> GetForClientAsync(
        DateOnly? targetDateFrom,
        DateOnly? targetDateTo,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> CancelAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public interface IWorkspaceOrderService
{
    Task<IReadOnlyList<OrderListItemResponse>> GetAllAsync(
        DateOnly? targetDate,
        Guid? clientCompanyId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<OrderListItemResponse> UpdateStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderListItemResponse> MarkReadyForDeliveryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

public sealed class ClientOrderService : IClientOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMenuItemOrderCatalog _menuItemCatalog;

    public ClientOrderService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IMenuItemOrderCatalog menuItemCatalog)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _menuItemCatalog = menuItemCatalog;
    }

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var userId = RequireUserId();

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new OrderServiceException("At least one order item is required.");
        }

        var normalizedItems = NormalizeItems(request.Items);
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

        var orderItems = normalizedItems.Select(input =>
        {
            var snapshot = snapshots[input.MenuItemId];
            var subtotal = snapshot.SellingPrice * input.Quantity;
            return new OrderItem
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MenuItemId = input.MenuItemId,
                Quantity = input.Quantity,
                UnitPrice = snapshot.SellingPrice,
                Subtotal = subtotal
            };
        }).ToList();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            PlacedByUserId = userId,
            TargetDate = request.TargetDate,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = orderItems.Sum(i => i.Subtotal),
            Items = orderItems
        };

        await _dbContext.Set<Order>().AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(order);
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetForClientAsync(
        DateOnly? targetDateFrom,
        DateOnly? targetDateTo,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var isAdmin = IsClientAdmin();

        var query = _dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.WorkspaceId == workspaceId && o.ClientCompanyId == clientCompanyId);

        if (!isAdmin)
        {
            var userId = RequireUserId();
            query = query.Where(o => o.PlacedByUserId == userId);
        }

        query = ApplyTargetDateFilter(query, targetDateFrom, targetDateTo);

        return await query
            .OrderByDescending(o => o.TargetDate)
            .ThenByDescending(o => o.CreatedAt)
            .Select(o => new OrderListItemResponse(
                o.Id,
                o.ClientCompanyId,
                o.PlacedByUserId,
                o.DriverId,
                o.TargetDate,
                o.CreatedAt,
                o.Status.ToString(),
                o.TotalAmount,
                o.Items.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderResponse> CancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var isAdmin = IsClientAdmin();
        var userId = _currentUser.UserId;

        var order = await _dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(
                o => o.Id == orderId
                    && o.WorkspaceId == workspaceId
                    && o.ClientCompanyId == clientCompanyId,
                cancellationToken);

        if (order is null)
        {
            throw new OrderServiceException($"Order '{orderId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (!isAdmin && order.PlacedByUserId != userId)
        {
            throw new OrderServiceException(
                "You can only cancel your own orders.",
                StatusCodes.Status403Forbidden);
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new OrderServiceException(
                "Only pending orders can be cancelled.",
                StatusCodes.Status409Conflict);
        }

        order.Status = OrderStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(order);
    }

    private static IReadOnlyList<CreateOrderItemInput> NormalizeItems(IReadOnlyList<CreateOrderItemInput> items)
    {
        var grouped = items
            .GroupBy(i => i.MenuItemId)
            .Select(g => new CreateOrderItemInput(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        if (grouped.Any(i => i.MenuItemId == Guid.Empty || i.Quantity <= 0))
        {
            throw new OrderServiceException("Each item must have a valid MenuItemId and Quantity > 0.");
        }

        return grouped;
    }

    private static IQueryable<Order> ApplyTargetDateFilter(
        IQueryable<Order> query,
        DateOnly? from,
        DateOnly? to)
    {
        if (from is DateOnly start)
        {
            query = query.Where(o => o.TargetDate >= start);
        }

        if (to is DateOnly end)
        {
            query = query.Where(o => o.TargetDate <= end);
        }

        return query;
    }

    private bool IsClientAdmin() =>
        string.Equals(_currentUser.Role, "ClientAdmin", StringComparison.OrdinalIgnoreCase);

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

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.Id,
            order.WorkspaceId,
            order.ClientCompanyId,
            order.PlacedByUserId,
            order.DriverId,
            order.TargetDate,
            order.CreatedAt,
            order.Status.ToString(),
            order.TotalAmount,
            order.Items.Select(i => new OrderItemResponse(
                i.Id,
                i.MenuItemId,
                i.Quantity,
                i.UnitPrice,
                i.Subtotal)).ToList());
}

public sealed class WorkspaceOrderService : IWorkspaceOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public WorkspaceOrderService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetAllAsync(
        DateOnly? targetDate,
        Guid? clientCompanyId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var query = _dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.WorkspaceId == workspaceId);

        if (targetDate is DateOnly date)
        {
            query = query.Where(o => o.TargetDate == date);
        }

        if (clientCompanyId is Guid companyId)
        {
            query = query.Where(o => o.ClientCompanyId == companyId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = ParseStatus(status);
            query = query.Where(o => o.Status == parsed);
        }

        return await query
            .OrderByDescending(o => o.TargetDate)
            .ThenByDescending(o => o.CreatedAt)
            .Select(o => new OrderListItemResponse(
                o.Id,
                o.ClientCompanyId,
                o.PlacedByUserId,
                o.DriverId,
                o.TargetDate,
                o.CreatedAt,
                o.Status.ToString(),
                o.TotalAmount,
                o.Items.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderListItemResponse> UpdateStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var newStatus = ParseStatus(request.Status);

        var order = await _dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WorkspaceId == workspaceId, cancellationToken);

        if (order is null)
        {
            throw new OrderServiceException($"Order '{orderId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (order.Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
        {
            throw new OrderServiceException("Cancelled orders cannot change status.", StatusCodes.Status409Conflict);
        }

        order.Status = newStatus;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OrderListItemResponse(
            order.Id,
            order.ClientCompanyId,
            order.PlacedByUserId,
            order.DriverId,
            order.TargetDate,
            order.CreatedAt,
            order.Status.ToString(),
            order.TotalAmount,
            order.Items.Count);
    }

    public async Task<OrderListItemResponse> MarkReadyForDeliveryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var order = await _dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WorkspaceId == workspaceId, cancellationToken);

        if (order is null)
        {
            throw new OrderServiceException($"Order '{orderId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (order.Status != OrderStatus.InProduction)
        {
            throw new OrderServiceException(
                "Only orders in InProduction can be marked ReadyForDelivery.",
                StatusCodes.Status409Conflict);
        }

        order.Status = OrderStatus.ReadyForDelivery;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OrderListItemResponse(
            order.Id,
            order.ClientCompanyId,
            order.PlacedByUserId,
            order.DriverId,
            order.TargetDate,
            order.CreatedAt,
            order.Status.ToString(),
            order.TotalAmount,
            order.Items.Count);
    }

    private static OrderStatus ParseStatus(string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
        {
            throw new OrderServiceException(
                $"Invalid status '{status}'. Allowed: {string.Join(", ", Enum.GetNames<OrderStatus>())}.");
        }

        return parsed;
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
}
