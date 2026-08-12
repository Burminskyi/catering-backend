namespace CateringSaaS.Modules.Ordering.DTOs;

public sealed record CreateOrderItemInput(Guid MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    DateOnly TargetDate,
    IReadOnlyList<CreateOrderItemInput> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid MenuItemId,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ClientCompanyId,
    Guid PlacedByUserId,
    DateOnly TargetDate,
    DateTime CreatedAt,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderListItemResponse(
    Guid Id,
    Guid ClientCompanyId,
    Guid PlacedByUserId,
    DateOnly TargetDate,
    DateTime CreatedAt,
    string Status,
    decimal TotalAmount,
    int ItemCount);

public sealed record UpdateOrderStatusRequest(string Status);
