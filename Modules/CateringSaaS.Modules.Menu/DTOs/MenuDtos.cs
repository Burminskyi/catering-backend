namespace CateringSaaS.Modules.Menu.DTOs;

public sealed record MenuListItemResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ClientCompanyId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);

public sealed record MenuDayItemResponse(
    Guid Id,
    Guid DishId,
    string DishName,
    string DishCategory,
    decimal SellingPrice);

public sealed record MenuDayResponse(
    Guid Id,
    DateOnly Date,
    IReadOnlyList<MenuDayItemResponse> Items);

public sealed record MenuDetailResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ClientCompanyId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    IReadOnlyList<MenuDayResponse> Days);

public sealed record CreateMenuRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ClientCompanyId);

public sealed record AddMenuItemRequest(Guid DishId, decimal SellingPrice);

public sealed record UpdateMenuStatusRequest(string Status);

/// <summary>
/// Client-portal menu line. <see cref="Date"/> is always set (YYYY-MM-DD) so PWA can filter by day
/// even if the parent <c>days[]</c> wrapper is flattened.
/// <see cref="OutputWeight"/> is null when unset/zero (do not show "0 г").
/// </summary>
public sealed record ClientPortalMenuItemResponse(
    Guid MenuItemId,
    Guid MenuId,
    string MenuName,
    Guid DishId,
    string DishName,
    string? Description,
    string Category,
    int? OutputWeight,
    decimal SellingPrice,
    DateOnly Date);

public sealed record ClientPortalMenuDayResponse(
    DateOnly Date,
    IReadOnlyList<ClientPortalMenuItemResponse> Items);
