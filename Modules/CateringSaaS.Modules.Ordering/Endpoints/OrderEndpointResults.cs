using CateringSaaS.Modules.Ordering.Services;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Ordering.Endpoints;

internal static class OrderEndpointResults
{
    public static IResult FromException(OrderServiceException ex) =>
        Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
}
