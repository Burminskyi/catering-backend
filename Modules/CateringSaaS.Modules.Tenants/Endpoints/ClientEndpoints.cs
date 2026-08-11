using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Modules.Tenants.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Tenants.Endpoints;

public static class ClientEndpoints
{
    public static RouteHandlerBuilder MapGetClientsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleGetAsync)
            .WithName("GetClients")
            .WithTags("Clients");
    }

    public static RouteHandlerBuilder MapCreateClientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleCreateAsync)
            .WithName("CreateClient")
            .WithTags("Clients");
    }

    private static async Task<IResult> HandleGetAsync(
        IClientCompanyService clientService,
        CancellationToken cancellationToken)
    {
        try
        {
            var clients = await clientService.GetAllAsync(cancellationToken);
            return Results.Ok(clients);
        }
        catch (TenantServiceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateClientCompanyRequest request,
        IClientCompanyService clientService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await clientService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/clients/{created.Id}", created);
        }
        catch (TenantServiceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }
}
