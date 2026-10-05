using CateringSaaS.Modules.Reporting.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Reporting.Endpoints;

public static class ReportingEndpoints
{
    public static RouteHandlerBuilder MapGetTodayPulseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/today-pulse", HandlePulseAsync)
            .WithName("GetTodayOperationsPulse")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetRevenueByClientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/revenue-by-client", HandleRevenueAsync)
            .WithName("GetRevenueByClientReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetStockMovementsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/stock-movements", HandleStockAsync)
            .WithName("GetStockMovementsReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetDeliveryAuditEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/delivery-audit", HandleAuditAsync)
            .WithName("GetDeliveryAuditReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetDishPopularityEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/dish-popularity", HandleDishesAsync)
            .WithName("GetDishPopularityReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetReclamationHeatMapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/reclamation-heatmap", HandleHeatMapAsync)
            .WithName("GetReclamationHeatMapReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetConsumptionVarianceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/consumption-variance", HandleVarianceAsync)
            .WithName("GetConsumptionVarianceReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetFoodCostEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/food-cost", HandleFoodCostAsync)
            .WithName("GetFoodCostReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetShortageForecastEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/shortage-forecast", HandleShortageAsync)
            .WithName("GetShortageForecastReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetDriverEfficiencyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/driver-efficiency", HandleDriversAsync)
            .WithName("GetDriverEfficiencyReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetSupplierSpendEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/supplier-spend", HandleSuppliersAsync)
            .WithName("GetSupplierSpendReport")
            .WithTags("Reports");
    }

    public static RouteHandlerBuilder MapGetCancellationsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/cancellations", HandleCancellationsAsync)
            .WithName("GetCancellationsReport")
            .WithTags("Reports");
    }

    private static async Task<IResult> HandlePulseAsync(
        DateOnly? targetDate,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => reporting.GetTodayPulseAsync(targetDate, cancellationToken));
    }

    private static async Task<IResult> HandleRevenueAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetRevenueByClientAsync(dateFrom, dateTo, clientCompanyId, cancellationToken));
    }

    private static async Task<IResult> HandleStockAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? ingredientId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetStockMovementsAsync(dateFrom, dateTo, ingredientId, cancellationToken));
    }

    private static async Task<IResult> HandleAuditAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetDeliveryAuditAsync(dateFrom, dateTo, clientCompanyId, cancellationToken));
    }

    private static async Task<IResult> HandleDishesAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetDishPopularityAsync(dateFrom, dateTo, clientCompanyId, cancellationToken));
    }

    private static async Task<IResult> HandleHeatMapAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        int? maxRating,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetReclamationHeatMapAsync(dateFrom, dateTo, clientCompanyId, maxRating, cancellationToken));
    }

    private static async Task<IResult> HandleVarianceAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? ingredientId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetConsumptionVarianceAsync(dateFrom, dateTo, ingredientId, cancellationToken));
    }

    private static async Task<IResult> HandleFoodCostAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => reporting.GetFoodCostAsync(dateFrom, dateTo, cancellationToken));
    }

    private static async Task<IResult> HandleShortageAsync(
        DateOnly? targetDate,
        bool? onlyDeficits,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetShortageForecastAsync(targetDate, onlyDeficits, cancellationToken));
    }

    private static async Task<IResult> HandleDriversAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? driverId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetDriverEfficiencyAsync(dateFrom, dateTo, driverId, cancellationToken));
    }

    private static async Task<IResult> HandleSuppliersAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? supplierId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetSupplierSpendAsync(dateFrom, dateTo, supplierId, cancellationToken));
    }

    private static async Task<IResult> HandleCancellationsAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        Guid? clientCompanyId,
        IReportingService reporting,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => reporting.GetCancellationsAsync(dateFrom, dateTo, clientCompanyId, cancellationToken));
    }

    private static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (ReportingServiceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
        }
    }
}
