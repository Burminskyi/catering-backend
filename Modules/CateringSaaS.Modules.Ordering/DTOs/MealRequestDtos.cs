namespace CateringSaaS.Modules.Ordering.DTOs;

public sealed record CreateMealRequestItemInput(Guid MenuItemId, int Quantity);

public sealed record CreateMealRequestRequest(
    DateOnly TargetDate,
    IReadOnlyList<CreateMealRequestItemInput> Items);

public sealed record MealRequestItemResponse(
    Guid Id,
    Guid MenuItemId,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record MealRequestResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ClientCompanyId,
    Guid EmployeeId,
    DateOnly TargetDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<MealRequestItemResponse> Items);

public sealed record MealRequestListItemResponse(
    Guid Id,
    Guid EmployeeId,
    DateOnly TargetDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    int ItemCount);

public sealed record ConsolidateMealRequestsRequest(DateOnly TargetDate);

public sealed record ConsolidateMealRequestsResponse(
    Guid OrderId,
    DateOnly TargetDate,
    int ProcessedRequestCount,
    decimal TotalAmount,
    string OrderStatus);

public sealed record CreateMealReviewRequest(
    DateOnly TargetDate,
    Guid MenuItemId,
    int Rating,
    string? Comment);

public sealed record MealReviewResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ClientCompanyId,
    Guid EmployeeId,
    DateOnly TargetDate,
    Guid MenuItemId,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

public sealed record AssignDriverRequest(Guid DriverId);
