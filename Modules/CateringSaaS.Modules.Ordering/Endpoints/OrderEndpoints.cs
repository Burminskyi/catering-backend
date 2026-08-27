using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Modules.Ordering.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Ordering.Endpoints;

public static class ClientOrderEndpoints
{
    public static RouteHandlerBuilder MapCreateClientOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/orders", HandleCreateAsync)
            .WithName("CreateClientOrder")
            .WithTags("ClientPortal");
    }

    public static RouteHandlerBuilder MapGetClientOrdersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/orders", HandleGetAsync)
            .WithName("GetClientOrders")
            .WithTags("ClientPortal");
    }

    public static RouteHandlerBuilder MapCancelClientOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/orders/{id:guid}/cancel", HandleCancelAsync)
            .WithName("CancelClientOrder")
            .WithTags("ClientPortal");
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateOrderRequest request,
        IClientOrderService orderService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await orderService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/client-portal/orders/{created.Id}", created);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleGetAsync(
        DateOnly? targetDateFrom,
        DateOnly? targetDateTo,
        IClientOrderService orderService,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await orderService.GetForClientAsync(
                targetDateFrom,
                targetDateTo,
                cancellationToken);
            return Results.Ok(orders);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleCancelAsync(
        Guid id,
        IClientOrderService orderService,
        CancellationToken cancellationToken)
    {
        try
        {
            var cancelled = await orderService.CancelAsync(id, cancellationToken);
            return Results.Ok(cancelled);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }
}

public static class WorkspaceOrderEndpoints
{
    public static RouteHandlerBuilder MapGetWorkspaceOrdersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", HandleGetAsync)
            .WithName("GetWorkspaceOrders")
            .WithTags("Orders");
    }

    public static RouteHandlerBuilder MapUpdateOrderStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/status", HandleUpdateStatusAsync)
            .WithName("UpdateOrderStatus")
            .WithTags("Orders");
    }

    public static RouteHandlerBuilder MapMarkOrderReadyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id:guid}/ready", HandleMarkReadyAsync)
            .WithName("MarkOrderReadyForDelivery")
            .WithTags("Orders");
    }

    private static async Task<IResult> HandleGetAsync(
        DateOnly? targetDate,
        Guid? clientCompanyId,
        string? status,
        IWorkspaceOrderService orderService,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await orderService.GetAllAsync(
                targetDate,
                clientCompanyId,
                status,
                cancellationToken);
            return Results.Ok(orders);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleUpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        IWorkspaceOrderService orderService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await orderService.UpdateStatusAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }

    private static async Task<IResult> HandleMarkReadyAsync(
        Guid id,
        IWorkspaceOrderService orderService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await orderService.MarkReadyForDeliveryAsync(id, cancellationToken);
            return Results.Ok(updated);
        }
        catch (OrderServiceException ex)
        {
            return OrderEndpointResults.FromException(ex);
        }
    }
}
