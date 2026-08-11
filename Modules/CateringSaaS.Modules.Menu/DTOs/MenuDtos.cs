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

public sealed record ClientPortalMenuItemResponse(
    Guid MenuItemId,
    Guid MenuId,
    string MenuName,
    Guid DishId,
    string DishName,
    string DishCategory,
    int OutputWeight,
    decimal SellingPrice);

public sealed record ClientPortalMenuDayResponse(
    DateOnly Date,
    IReadOnlyList<ClientPortalMenuItemResponse> Items);
