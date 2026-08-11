using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Modules.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Identity.Endpoints;

public static class ClientEmployeeEndpoints
{
    public static RouteHandlerBuilder MapGetClientEmployeesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleGetAsync)
            .WithName("GetClientEmployees")
            .WithTags("ClientEmployees");
    }

    public static RouteHandlerBuilder MapCreateClientEmployeeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleCreateAsync)
            .WithName("CreateClientEmployee")
            .WithTags("ClientEmployees");
    }

    private static async Task<IResult> HandleGetAsync(
        IClientEmployeeService clientEmployeeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var employees = await clientEmployeeService.GetAllAsync(cancellationToken);
            return Results.Ok(employees);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateClientEmployeeRequest request,
        IClientEmployeeService clientEmployeeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await clientEmployeeService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/client-employees/{created.Id}", created);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}
