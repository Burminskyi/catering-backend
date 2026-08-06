using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Modules.Inventory.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class UpdateIngredientEndpoint
{
    public static RouteHandlerBuilder MapUpdateIngredientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateIngredient")
            .WithTags("Ingredients");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateIngredientRequest request,
        IValidator<UpdateIngredientRequest> validator,
        IIngredientService ingredientService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var updated = await ingredientService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
