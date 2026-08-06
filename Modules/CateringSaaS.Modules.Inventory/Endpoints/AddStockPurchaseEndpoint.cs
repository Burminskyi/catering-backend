using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Modules.Inventory.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class AddStockPurchaseEndpoint
{
    public static RouteHandlerBuilder MapAddStockPurchaseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/purchases", HandleAsync)
            .WithName("AddStockPurchase")
            .WithTags("Inventory");
    }

    private static async Task<IResult> HandleAsync(
        AddStockPurchaseRequest request,
        IValidator<AddStockPurchaseRequest> validator,
        IStockPurchaseService purchaseService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var result = await purchaseService.AddPurchaseAsync(request, cancellationToken);
            return Results.Created($"/api/inventory/purchases/{result.BatchId}", result);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
