using CateringSaaS.Modules.Kitchen.Services;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Kitchen.Endpoints;

internal static class KitchenEndpointResults
{
    public static IResult FromException(KitchenServiceException ex)
    {
        if (ex.Shortages is { Count: > 0 })
        {
            return Results.Json(
                new { message = ex.Message, shortages = ex.Shortages },
                statusCode: ex.StatusCode);
        }

        return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
    }
}
