using CateringSaaS.Modules.Menu.Domain;
using CateringSaaS.Modules.Menu.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MenuEntity = CateringSaaS.Modules.Menu.Domain.Menu;

namespace CateringSaaS.Modules.Menu.Services;

public interface IMenuService
{
    Task<IReadOnlyList<MenuListItemResponse>> GetAllAsync(
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default);

    Task<MenuDetailResponse> CreateAsync(
        CreateMenuRequest request,
        CancellationToken cancellationToken = default);

    Task<MenuDayItemResponse> AddItemAsync(
        Guid menuId,
        DateOnly date,
        AddMenuItemRequest request,
        CancellationToken cancellationToken = default);

    Task<MenuListItemResponse> UpdateStatusAsync(
        Guid menuId,
        UpdateMenuStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientPortalMenuDayResponse>> GetActiveForClientAsync(
        CancellationToken cancellationToken = default);
}

public sealed class MenuService : IMenuService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClientCompanyLookup _clientCompanyLookup;

    public MenuService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IClientCompanyLookup clientCompanyLookup)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _clientCompanyLookup = clientCompanyLookup;
    }

    public async Task<IReadOnlyList<MenuListItemResponse>> GetAllAsync(
        Guid? clientCompanyId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var query = _dbContext.Set<MenuEntity>()
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId);

        if (clientCompanyId is Guid filterCompanyId)
        {
            query = query.Where(m => m.ClientCompanyId == filterCompanyId);
        }

        return await query
            .OrderByDescending(m => m.StartDate)
            .ThenBy(m => m.Name)
            .Select(m => new MenuListItemResponse(
                m.Id,
                m.WorkspaceId,
                m.ClientCompanyId,
                m.Name,
                m.StartDate,
                m.EndDate,
                m.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuDetailResponse> CreateAsync(
        CreateMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new MenuServiceException("Name is required.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new MenuServiceException("EndDate must be on or after StartDate.");
        }

        if (request.ClientCompanyId is Guid clientCompanyId)
        {
            var exists = await _clientCompanyLookup.ExistsInWorkspaceAsync(
                clientCompanyId,
                workspaceId,
                cancellationToken);

            if (!exists)
            {
                throw new MenuServiceException(
                    $"Client company '{clientCompanyId}' was not found in this workspace.",
                    StatusCodes.Status400BadRequest);
            }
        }

        var menu = new MenuEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = request.ClientCompanyId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = MenuStatus.Draft,
            Days = BuildDays(workspaceId, request.StartDate, request.EndDate)
        };

        await _dbContext.Set<MenuEntity>().AddAsync(menu, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDetail(menu);
    }

    public async Task<MenuDayItemResponse> AddItemAsync(
        Guid menuId,
        DateOnly date,
        AddMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        if (request.DishId == Guid.Empty)
        {
            throw new MenuServiceException("DishId is required.");
        }

        if (request.SellingPrice < 0)
        {
            throw new MenuServiceException("SellingPrice cannot be negative.");
        }

        var menu = await _dbContext.Set<MenuEntity>()
            .Include(m => m.Days)
            .ThenInclude(d => d.Items)
            .FirstOrDefaultAsync(m => m.Id == menuId && m.WorkspaceId == workspaceId, cancellationToken);

        if (menu is null)
        {
            throw new MenuServiceException($"Menu '{menuId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (date < menu.StartDate || date > menu.EndDate)
        {
            throw new MenuServiceException(
                $"Date {date:yyyy-MM-dd} is outside the menu range {menu.StartDate:yyyy-MM-dd}..{menu.EndDate:yyyy-MM-dd}.");
        }

        var dish = await _dbContext.Set<Dish>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == request.DishId && d.WorkspaceId == workspaceId && d.IsActive,
                cancellationToken);

        if (dish is null)
        {
            throw new MenuServiceException(
                $"Active dish '{request.DishId}' was not found.",
                StatusCodes.Status400BadRequest);
        }

        var day = menu.Days.FirstOrDefault(d => d.Date == date);
        if (day is null)
        {
            day = new MenuDay
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MenuId = menu.Id,
                Date = date,
                Items = new List<MenuItem>()
            };
            menu.Days.Add(day);
            await _dbContext.Set<MenuDay>().AddAsync(day, cancellationToken);
        }

        if (day.Items.Any(i => i.DishId == request.DishId))
        {
            throw new MenuServiceException(
                "This dish is already on the menu for that date.",
                StatusCodes.Status409Conflict);
        }

        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            MenuDayId = day.Id,
            DishId = request.DishId,
            SellingPrice = request.SellingPrice
        };

        await _dbContext.Set<MenuItem>().AddAsync(item, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MenuDayItemResponse(
            item.Id,
            dish.Id,
            dish.Name,
            dish.Category.ToString(),
            item.SellingPrice);
    }

    public async Task<MenuListItemResponse> UpdateStatusAsync(
        Guid menuId,
        UpdateMenuStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var status = ParseStatus(request.Status);

        var menu = await _dbContext.Set<MenuEntity>()
            .FirstOrDefaultAsync(m => m.Id == menuId && m.WorkspaceId == workspaceId, cancellationToken);

        if (menu is null)
        {
            throw new MenuServiceException($"Menu '{menuId}' was not found.", StatusCodes.Status404NotFound);
        }

        menu.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MenuListItemResponse(
            menu.Id,
            menu.WorkspaceId,
            menu.ClientCompanyId,
            menu.Name,
            menu.StartDate,
            menu.EndDate,
            menu.Status.ToString());
    }

    public async Task<IReadOnlyList<ClientPortalMenuDayResponse>> GetActiveForClientAsync(
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rows = await _dbContext.Set<MenuItem>()
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId)
            .Where(i =>
                i.MenuDay.Menu.Status == MenuStatus.Published
                && (i.MenuDay.Menu.ClientCompanyId == null
                    || i.MenuDay.Menu.ClientCompanyId == clientCompanyId)
                && i.MenuDay.Date >= i.MenuDay.Menu.StartDate
                && i.MenuDay.Date <= i.MenuDay.Menu.EndDate
                && i.MenuDay.Date >= today)
            .Select(i => new
            {
                i.Id,
                MenuId = i.MenuDay.MenuId,
                MenuName = i.MenuDay.Menu.Name,
                Date = i.MenuDay.Date,
                i.DishId,
                DishName = i.Dish.Name,
                DishCategory = i.Dish.Category,
                OutputWeight = i.Dish.OutputWeight,
                i.SellingPrice
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.DishName)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.Date)
            .Select(g => new ClientPortalMenuDayResponse(
                g.Key,
                g.Select(x => new ClientPortalMenuItemResponse(
                    x.Id,
                    x.MenuId,
                    x.MenuName,
                    x.DishId,
                    x.DishName,
                    x.DishCategory.ToString(),
                    x.OutputWeight,
                    x.SellingPrice)).ToList()))
            .ToList();
    }

    private static List<MenuDay> BuildDays(Guid workspaceId, DateOnly start, DateOnly end)
    {
        var days = new List<MenuDay>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            days.Add(new MenuDay
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Date = date,
                Items = new List<MenuItem>()
            });
        }

        return days;
    }

    private static MenuDetailResponse ToDetail(MenuEntity menu) =>
        new(
            menu.Id,
            menu.WorkspaceId,
            menu.ClientCompanyId,
            menu.Name,
            menu.StartDate,
            menu.EndDate,
            menu.Status.ToString(),
            menu.Days
                .OrderBy(d => d.Date)
                .Select(d => new MenuDayResponse(
                    d.Id,
                    d.Date,
                    d.Items
                        .Select(i => new MenuDayItemResponse(
                            i.Id,
                            i.DishId,
                            i.Dish?.Name ?? string.Empty,
                            i.Dish?.Category.ToString() ?? string.Empty,
                            i.SellingPrice))
                        .ToList()))
                .ToList());

    private static MenuStatus ParseStatus(string status)
    {
        if (!Enum.TryParse<MenuStatus>(status, ignoreCase: true, out var parsed))
        {
            throw new MenuServiceException(
                $"Invalid status '{status}'. Allowed: {string.Join(", ", Enum.GetNames<MenuStatus>())}.");
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

    private Guid RequireClientCompany()
    {
        if (_currentUser.ClientCompanyId is not Guid clientCompanyId || clientCompanyId == Guid.Empty)
        {
            throw new MenuServiceException(
                "Client company context is required.",
                StatusCodes.Status400BadRequest);
        }

        return clientCompanyId;
    }
}
