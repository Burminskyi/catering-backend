using CateringSaaS.Modules.Inventory.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class ImportIngredientsCsvEndpoint
{
    private const long MaxFileBytes = 2 * 1024 * 1024;

    public static RouteHandlerBuilder MapImportIngredientsCsvEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/ingredients/import-csv", HandleAsync)
            .WithName("ImportIngredientsCsv")
            .WithTags("Inventory")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data");
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        IIngredientService ingredientService,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "CSV file is required." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "CSV file is required." });
        }

        if (file.Length > MaxFileBytes)
        {
            return Results.BadRequest(new { message = "CSV file must be 2 MB or smaller." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.IsNullOrEmpty(extension)
            && !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Only CSV files are supported." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var summary = await ingredientService.ImportCsvAsync(stream, cancellationToken);
            return Results.Ok(summary);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
