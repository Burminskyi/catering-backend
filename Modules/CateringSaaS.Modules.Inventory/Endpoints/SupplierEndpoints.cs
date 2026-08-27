using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Modules.Inventory.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class SupplierEndpoints
{
    public static RouteHandlerBuilder MapGetSuppliersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleGetAsync)
            .WithName("GetSuppliers")
            .WithTags("Suppliers");
    }

    public static RouteHandlerBuilder MapCreateSupplierEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleCreateAsync)
            .WithName("CreateSupplier")
            .WithTags("Suppliers");
    }

    public static RouteHandlerBuilder MapUpdateSupplierEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}", HandleUpdateAsync)
            .WithName("UpdateSupplier")
            .WithTags("Suppliers");
    }

    public static RouteHandlerBuilder MapDeleteSupplierEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{id:guid}", HandleDeleteAsync)
            .WithName("DeleteSupplier")
            .WithTags("Suppliers");
    }

    private static async Task<IResult> HandleGetAsync(
        bool? includeInactive,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await supplierService.GetAllAsync(includeInactive ?? false, cancellationToken);
            return Results.Ok(items);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateSupplierRequest request,
        IValidator<CreateSupplierRequest> validator,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var created = await supplierService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/suppliers/{created.Id}", created);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleUpdateAsync(
        Guid id,
        UpdateSupplierRequest request,
        IValidator<UpdateSupplierRequest> validator,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var updated = await supplierService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleDeleteAsync(
        Guid id,
        ISupplierService supplierService,
        CancellationToken cancellationToken)
    {
        try
        {
            await supplierService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}

public static class InventoryMovementEndpoints
{
    public static RouteHandlerBuilder MapGetInventoryMovementsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/movements", HandleAsync)
            .WithName("GetInventoryMovements")
            .WithTags("Inventory");
    }

    private static async Task<IResult> HandleAsync(
        int? page,
        int? pageSize,
        Guid? ingredientId,
        string? type,
        DateTime? from,
        DateTime? to,
        IInventoryMovementService movementService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await movementService.GetMovementsAsync(
                page ?? 1,
                pageSize ?? 20,
                ingredientId,
                type,
                from,
                to,
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
