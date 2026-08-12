using CateringSaaS.Modules.Kitchen.DTOs;
using CateringSaaS.Modules.Kitchen.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Kitchen.Endpoints;

public static class KitchenPlanEndpoints
{
    public static RouteHandlerBuilder MapGetProductionPlanEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/plan", HandleGetAsync)
            .WithName("GetProductionPlan")
            .WithTags("Kitchen");
    }

    public static RouteHandlerBuilder MapExecuteProductionPlanEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/plan/execute", HandleExecuteAsync)
            .WithName("ExecuteProductionPlan")
            .WithTags("Kitchen");
    }

    private static async Task<IResult> HandleGetAsync(
        DateOnly targetDate,
        IProductionPlanService productionPlanService,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await productionPlanService.GetPlanAsync(targetDate, cancellationToken);
            return Results.Ok(plan);
        }
        catch (KitchenServiceException ex)
        {
            return KitchenEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleExecuteAsync(
        ExecuteProductionPlanRequest request,
        IProductionPlanService productionPlanService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await productionPlanService.ExecutePlanAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (KitchenServiceException ex)
        {
            return KitchenEndpointResults.FromException(ex);
        }
    }
}
