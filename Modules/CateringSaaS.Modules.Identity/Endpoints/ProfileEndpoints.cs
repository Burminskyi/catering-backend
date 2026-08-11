using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Modules.Identity.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Identity.Endpoints;

public static class GetProfileEndpoint
{
    public static RouteHandlerBuilder MapGetProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleAsync)
            .WithName("GetProfile")
            .WithTags("Profile");
    }

    private static async Task<IResult> HandleAsync(
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileService.GetAsync(cancellationToken);
            return Results.Ok(profile);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}

public static class UpdateProfileEndpoint
{
    public static RouteHandlerBuilder MapUpdateProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/", HandleAsync)
            .WithName("UpdateProfile")
            .WithTags("Profile");
    }

    private static async Task<IResult> HandleAsync(
        UpdateProfileRequest request,
        IValidator<UpdateProfileRequest> validator,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var profile = await profileService.UpdateAsync(request, cancellationToken);
            return Results.Ok(profile);
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}

public static class ChangePasswordEndpoint
{
    public static RouteHandlerBuilder MapChangePasswordEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/password", HandleAsync)
            .WithName("ChangePassword")
            .WithTags("Profile");
    }

    private static async Task<IResult> HandleAsync(
        ChangePasswordRequest request,
        IValidator<ChangePasswordRequest> validator,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            await profileService.ChangePasswordAsync(request, cancellationToken);
            return Results.NoContent();
        }
        catch (IdentityServiceException ex)
        {
            return IdentityEndpointResults.FromException(ex);
        }
    }
}
