using CateringSaaS.Modules.Inventory.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class DeleteIngredientEndpoint
{
    public static RouteHandlerBuilder MapDeleteIngredientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteIngredient")
            .WithTags("Ingredients");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IIngredientService ingredientService,
        CancellationToken cancellationToken)
    {
        try
        {
            await ingredientService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
