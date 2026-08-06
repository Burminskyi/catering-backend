using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class CreateIngredientEndpoint
{
    public static RouteHandlerBuilder MapCreateIngredientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/ingredients", HandleAsync)
            .WithName("CreateIngredient")
            .WithTags("Inventory")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        CreateIngredientRequest request,
        IValidator<CreateIngredientRequest> validator,
        AppDbContext dbContext,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        if (tenantContext.WorkspaceId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Workspace context is required to create an ingredient." });
        }

        if (!Enum.TryParse<IngredientCategory>(request.Category, ignoreCase: true, out var category))
        {
            return Results.BadRequest(new { message = $"Invalid Category '{request.Category}'." });
        }

        if (!Enum.TryParse<UnitOfMeasure>(request.BaseUnit, ignoreCase: true, out var baseUnit))
        {
            return Results.BadRequest(new { message = $"Invalid BaseUnit '{request.BaseUnit}'." });
        }

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Category = category,
            BaseUnit = baseUnit,
            WorkspaceId = tenantContext.WorkspaceId
        };

        await dbContext.Set<Ingredient>().AddAsync(ingredient, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/ingredients/{ingredient.Id}",
            new IngredientResponse(
                ingredient.Id,
                ingredient.Name,
                ingredient.Category.ToString(),
                ingredient.BaseUnit.ToString(),
                ingredient.WorkspaceId,
                IsGlobal: false));
    }
}
