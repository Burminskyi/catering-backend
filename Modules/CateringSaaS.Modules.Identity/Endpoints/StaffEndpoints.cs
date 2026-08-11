using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Modules.Identity.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Identity.Endpoints;

public static class GetStaffEndpoint
{
    public static RouteHandlerBuilder MapGetStaffEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetStaff")
            .WithTags("Staff");
    }

    private static async Task<IResult> HandleAsync(
        IStaffService staffService,
        CancellationToken cancellationToken)
    {
        try
        {
            var staff = await staffService.GetAllAsync(cancellationToken);
            return Results.Ok(staff);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}

public static class CreateStaffEndpoint
{
    public static RouteHandlerBuilder MapCreateStaffEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", HandleAsync)
            .WithName("CreateStaff")
            .WithTags("Staff");
    }

    private static async Task<IResult> HandleAsync(
        CreateStaffMemberRequest request,
        IValidator<CreateStaffMemberRequest> validator,
        IStaffService staffService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var created = await staffService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/staff/{created.Id}", created);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}

public static class UpdateStaffEndpoint
{
    public static RouteHandlerBuilder MapUpdateStaffEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateStaff")
            .WithTags("Staff");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateStaffMemberRequest request,
        IValidator<UpdateStaffMemberRequest> validator,
        IStaffService staffService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var updated = await staffService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}

public static class DeleteStaffEndpoint
{
    public static RouteHandlerBuilder MapDeleteStaffEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteStaff")
            .WithTags("Staff");
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IStaffService staffService,
        CancellationToken cancellationToken)
    {
        try
        {
            await staffService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}
