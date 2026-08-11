using CateringSaaS.Modules.Menu.DTOs;
using CateringSaaS.Modules.Menu.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Menu.Endpoints;

public static class GetDishesEndpoint
{
    public static RouteHandlerBuilder MapGetDishesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetDishes")
            .WithTags("Dishes");
    }

    private static async Task<IResult> HandleAsync(
        IDishService dishService,
        CancellationToken cancellationToken)
    {
        try
        {
            var dishes = await dishService.GetActiveAsync(cancellationToken);
            return Results.Ok(dishes);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class CreateDishEndpoint
{
    public static RouteHandlerBuilder MapCreateDishEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleAsync)
            .WithName("CreateDish")
            .WithTags("Dishes");
    }

    private static async Task<IResult> HandleAsync(
        CreateDishRequest request,
        IDishService dishService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await dishService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/dishes/{created.Id}", created);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class UpdateDishEndpoint
{
    public static RouteHandlerBuilder MapUpdateDishEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateDish")
            .WithTags("Dishes");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateDishRequest request,
        IDishService dishService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await dishService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}

public static class DeleteDishEndpoint
{
    public static RouteHandlerBuilder MapDeleteDishEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteDish")
            .WithTags("Dishes");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IDishService dishService,
        CancellationToken cancellationToken)
    {
        try
        {
            await dishService.SoftDeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (MenuServiceException ex)
        {
            return MenuEndpointResults.FromException(ex);
        }
    }
}
