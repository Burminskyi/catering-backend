using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Modules.Ordering.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Ordering.Endpoints;

public static class MealRequestEndpoints
{
    public static RouteHandlerBuilder MapCreateMealRequestEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/meal-requests", HandleCreateAsync)
            .WithName("CreateEmployeeMealRequest")
            .WithTags("ClientPortal");
    }

    public static RouteHandlerBuilder MapGetMyMealRequestsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/meal-requests/my", HandleGetMyAsync)
            .WithName("GetMyMealRequests")
            .WithTags("ClientPortal");
    }

    public static RouteHandlerBuilder MapGetSubmittedMealRequestsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/meal-requests", HandleGetSubmittedAsync)
            .WithName("GetSubmittedMealRequests")
            .WithTags("ClientPortal");
    }

    public static RouteHandlerBuilder MapConsolidateMealRequestsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/meal-requests/consolidate", HandleConsolidateAsync)
            .WithName("ConsolidateMealRequests")
            .WithTags("ClientPortal");
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateMealRequestRequest request,
        IEmployeeMealRequestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/client-portal/meal-requests/{created.Id}", created);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleGetMyAsync(
        IEmployeeMealRequestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await service.GetMyAsync(cancellationToken);
            return Results.Ok(items);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleGetSubmittedAsync(
        DateOnly targetDate,
        IClientAdminMealRequestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await service.GetSubmittedForDateAsync(targetDate, cancellationToken);
            return Results.Ok(items);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleConsolidateAsync(
        ConsolidateMealRequestsRequest request,
        IClientAdminMealRequestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ConsolidateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }
}

public static class MealReviewEndpoints
{
    public static RouteHandlerBuilder MapCreateMealReviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/reviews", HandleCreateAsync)
            .WithName("CreateMealReview")
            .WithTags("ClientPortal");
    }

    public static RouteHandlerBuilder MapGetWorkspaceReviewsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleGetWorkspaceAsync)
            .WithName("GetWorkspaceReviews")
            .WithTags("Reviews");
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateMealReviewRequest request,
        IMealReviewService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/client-portal/reviews/{created.Id}", created);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleGetWorkspaceAsync(
        bool? isReclamation,
        IMealReviewService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var reviews = await service.GetForWorkspaceAsync(isReclamation, cancellationToken);
            return Results.Ok(reviews);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }
}

public static class DeliveryEndpoints
{
    public static RouteHandlerBuilder MapAssignDriverEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/assign-driver", HandleAssignAsync)
            .WithName("AssignOrderDriver")
            .WithTags("Orders");
    }

    public static RouteHandlerBuilder MapGetMyDeliveryOrdersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/my-orders", HandleGetMyAsync)
            .WithName("GetMyDeliveryOrders")
            .WithTags("Delivery");
    }

    public static RouteHandlerBuilder MapDeliverOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/orders/{id:guid}/deliver", HandleDeliverAsync)
            .WithName("DeliverOrder")
            .WithTags("Delivery");
    }

    private static async Task<IResult> HandleAssignAsync(
        Guid id,
        AssignDriverRequest request,
        IDeliveryService deliveryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await deliveryService.AssignDriverAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleGetMyAsync(
        IDeliveryService deliveryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await deliveryService.GetMyOrdersAsync(cancellationToken);
            return Results.Ok(orders);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleDeliverAsync(
        Guid id,
        IDeliveryService deliveryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await deliveryService.MarkDeliveredAsync(id, cancellationToken);
            return Results.Ok(updated);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }
}
