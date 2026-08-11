using CateringSaaS.Modules.Menu.Services;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Menu.Endpoints;

internal static class MenuEndpointResults
{
    public static IResult FromException(MenuServiceException ex) =>
        Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
}
