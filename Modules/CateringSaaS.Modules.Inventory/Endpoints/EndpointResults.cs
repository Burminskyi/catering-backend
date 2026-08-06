using CateringSaaS.Modules.Inventory.Services;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Inventory.Endpoints;

internal static class EndpointResults
{
    public static IResult FromException(ServiceException ex) =>
        Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
}
