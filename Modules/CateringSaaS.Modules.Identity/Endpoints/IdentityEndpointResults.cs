using CateringSaaS.Modules.Identity.Services;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Identity.Endpoints;

internal static class IdentityEndpointResults
{
    public static IResult FromException(IdentityServiceException ex) =>
        Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
}
