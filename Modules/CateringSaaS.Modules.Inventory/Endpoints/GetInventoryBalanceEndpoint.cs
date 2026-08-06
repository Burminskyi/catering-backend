using CateringSaaS.Modules.Inventory.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Inventory.Endpoints;

public static class GetInventoryBalanceEndpoint
{
    public static RouteHandlerBuilder MapGetInventoryBalanceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/balance", HandleAsync)
            .WithName("GetInventoryBalance")
            .WithTags("Inventory");
    }

    private static async Task<IResult> HandleAsync(
        IInventoryBalanceService balanceService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await balanceService.GetBalanceAsync(cancellationToken);
            return Results.Ok(result);
        }
        catch (ServiceException ex)
        {
            return EndpointResults.FromException(ex);
        }
    }
}
