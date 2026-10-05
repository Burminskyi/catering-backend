using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Modules.Tenants.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class UpdateWorkspaceBrandingEndpoint
{
    public static RouteHandlerBuilder MapUpdateWorkspaceBrandingEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/", HandleAsync)
            .WithName("UpdateWorkspaceBranding")
            .WithTags("Workspace");
    }

    private static async Task<IResult> HandleAsync(
        UpdateWorkspaceBrandingRequest request,
        IWorkspaceBrandingService brandingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var workspace = await brandingService.UpdateCurrentAsync(request, cancellationToken);
            return Results.Ok(workspace);
        }
        catch (WorkspaceBrandingException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
        }
    }
}
