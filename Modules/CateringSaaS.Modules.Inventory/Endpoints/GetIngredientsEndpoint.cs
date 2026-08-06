using CateringSaaS.Modules.Inventory.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class GetIngredientsEndpoint
{
    public static RouteHandlerBuilder MapGetIngredientsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetIngredients")
            .WithTags("Ingredients");
    }

    private static async Task<IResult> HandleAsync(
        IIngredientService ingredientService,
        string? search,
        string? category,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ingredientService.GetIngredientsAsync(
                search,
                category,
                page ?? 1,
                pageSize ?? 50,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
