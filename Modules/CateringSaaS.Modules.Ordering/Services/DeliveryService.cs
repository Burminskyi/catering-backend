using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public interface IDeliveryService
{
    Task<OrderListItemResponse> AssignDriverAsync(
        Guid orderId,
        AssignDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItemResponse>> GetMyOrdersAsync(
        CancellationToken cancellationToken = default);

    Task<OrderListItemResponse> MarkDeliveredAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

public sealed class DeliveryService : IDeliveryService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IPushNotificationService _pushNotificationService;

    public DeliveryService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IPushNotificationService pushNotificationService)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<OrderListItemResponse> AssignDriverAsync(
        Guid orderId,
        AssignDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        if (request.DriverId == Guid.Empty)
        {
            throw new OrderServiceException("DriverId is required.");
        }

        var order = await _dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.WorkspaceId == workspaceId, cancellationToken);

        if (order is null)
        {
            throw new OrderServiceException($"Order '{orderId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
        {
            throw new OrderServiceException(
                $"Cannot assign a driver to an order in status '{order.Status}'.",
                StatusCodes.Status409Conflict);
        }

        order.DriverId = request.DriverId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToListItem(order);
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetMyOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureDriver();
        var workspaceId = RequireWorkspace();
        var driverId = RequireUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.WorkspaceId == workspaceId
                && o.DriverId == driverId
                && o.TargetDate == today
                && (o.Status == OrderStatus.ReadyForDelivery || o.Status == OrderStatus.Delivered))
            .OrderBy(o => o.Status)
            .ThenBy(o => o.ClientCompanyId)
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

    public async Task<OrderListItemResponse> MarkDeliveredAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        EnsureDriver();
        var workspaceId = RequireWorkspace();
        var driverId = RequireUserId();

        var order = await _dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(
                o => o.Id == orderId
                    && o.WorkspaceId == workspaceId
                    && o.DriverId == driverId,
                cancellationToken);

        if (order is null)
        {
            throw new OrderServiceException($"Order '{orderId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (order.Status == OrderStatus.Delivered)
        {
            throw new OrderServiceException("Order is already delivered.", StatusCodes.Status409Conflict);
        }

        if (order.Status != OrderStatus.ReadyForDelivery)
        {
            throw new OrderServiceException(
                "Only orders in ReadyForDelivery can be marked as Delivered.",
                StatusCodes.Status409Conflict);
        }

        order.Status = OrderStatus.Delivered;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _pushNotificationService.NotifyEmployeesOrderDeliveredAsync(
            order.ClientCompanyId,
            order.TargetDate,
            cancellationToken);

        return ToListItem(order);
    }

    private void EnsureDriver()
    {
        if (!string.Equals(_currentUser.Role, "Driver", StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderServiceException(
                "Only Driver can access delivery operations.",
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

    private static OrderListItemResponse ToListItem(Order order) =>
        new(
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
