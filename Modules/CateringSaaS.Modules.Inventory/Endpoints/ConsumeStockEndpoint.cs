using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Modules.Inventory.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class ConsumeStockEndpoint
{
    public static RouteHandlerBuilder MapConsumeStockEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/consume", HandleAsync)
            .WithName("ConsumeStock")
            .WithTags("Inventory");
    }

    private static async Task<IResult> HandleAsync(
        ConsumeStockRequest request,
        IValidator<ConsumeStockRequest> validator,
        IStockConsumptionService consumptionService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var result = await consumptionService.ConsumeAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
