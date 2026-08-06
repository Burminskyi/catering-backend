using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Modules.Inventory.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class CreateIngredientEndpoint
{
    public static RouteHandlerBuilder MapCreateIngredientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleAsync)
            .WithName("CreateIngredient")
            .WithTags("Ingredients");
    }

    private static async Task<IResult> HandleAsync(
        CreateIngredientRequest request,
        IValidator<CreateIngredientRequest> validator,
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
            var created = await ingredientService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/ingredients/{created.Id}", created);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
