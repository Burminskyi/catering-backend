using CateringSaaS.Modules.Menu.DTOs;
using CateringSaaS.Modules.Menu.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Menu.Endpoints;

public static class GetMenusEndpoint
{
    public static RouteHandlerBuilder MapGetMenusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetMenus")
            .WithTags("Menus");
    }

    private static async Task<IResult> HandleAsync(
        Guid? clientCompanyId,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        try
        {
            var menus = await menuService.GetAllAsync(clientCompanyId, cancellationToken);
            return Results.Ok(menus);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class CreateMenuEndpoint
{
    public static RouteHandlerBuilder MapCreateMenuEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleAsync)
            .WithName("CreateMenu")
            .WithTags("Menus");
    }

    private static async Task<IResult> HandleAsync(
        CreateMenuRequest request,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await menuService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/menus/{created.Id}", created);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class AddMenuItemEndpoint
{
    public static RouteHandlerBuilder MapAddMenuItemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id:guid}/days/{date}/items", HandleAsync)
            .WithName("AddMenuItem")
            .WithTags("Menus");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        DateOnly date,
        AddMenuItemRequest request,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await menuService.AddItemAsync(id, date, request, cancellationToken);
            return Results.Created($"/api/menus/{id}/days/{date:yyyy-MM-dd}/items/{item.Id}", item);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class UpdateMenuStatusEndpoint
{
    public static RouteHandlerBuilder MapUpdateMenuStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/status", HandleAsync)
            .WithName("UpdateMenuStatus")
            .WithTags("Menus");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateMenuStatusRequest request,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await menuService.UpdateStatusAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class GetActiveClientMenusEndpoint
{
    public static RouteHandlerBuilder MapGetActiveClientMenusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/menus/active", HandleAsync)
            .WithName("GetActiveClientMenus")
            .WithTags("ClientPortal");
    }

    private static async Task<IResult> HandleAsync(
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        try
        {
            var days = await menuService.GetActiveForClientAsync(cancellationToken);
            return Results.Ok(days);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}
