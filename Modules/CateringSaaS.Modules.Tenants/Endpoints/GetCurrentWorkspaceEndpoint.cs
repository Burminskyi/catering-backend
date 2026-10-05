using CateringSaaS.Modules.Tenants.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class GetCurrentWorkspaceEndpoint
{
    public static RouteHandlerBuilder MapGetCurrentWorkspaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetCurrentWorkspace")
            .WithTags("Workspace");
    }

    private static async Task<IResult> HandleAsync(
        IWorkspaceBrandingService brandingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var workspace = await brandingService.GetCurrentAsync(cancellationToken);
            return Results.Ok(workspace);
        }
        catch (WorkspaceBrandingException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
        }
    }
}
