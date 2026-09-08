namespace CateringSaaS.Modules.Ordering.DTOs;

public sealed record CreateOrderItemInput(Guid MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    DateOnly TargetDate,
    IReadOnlyList<CreateOrderItemInput> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid MenuItemId,
    Guid? DishId,
    string DishName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ClientCompanyId,
    Guid PlacedByUserId,
    Guid? DriverId,
    DateOnly TargetDate,
    DateTime CreatedAt,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    int Portions,
    IReadOnlyList<OrderItemResponse> Items);

/// <summary>
/// <c>ItemCount</c> = number of lines; <c>Portions</c> = sum(items.quantity).
/// </summary>
public sealed record OrderListItemResponse(
    Guid Id,
    Guid ClientCompanyId,
    Guid PlacedByUserId,
    Guid? DriverId,
    DateOnly TargetDate,
    DateTime CreatedAt,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    int Portions,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record UpdateOrderStatusRequest(string Status);
