using CateringSaaS.Modules.Reporting.Endpoints;
using CateringSaaS.Modules.Reporting.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Reporting;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IReportingService, ReportingService>();
        return services;
    }

    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/reports")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "Chef",
                "SuperAdmin"));

        reports.MapGetTodayPulseEndpoint();
        reports.MapGetRevenueByClientEndpoint();
        reports.MapGetStockMovementsEndpoint();
        reports.MapGetDeliveryAuditEndpoint();
        reports.MapGetDishPopularityEndpoint();
        reports.MapGetReclamationHeatMapEndpoint();
        reports.MapGetConsumptionVarianceEndpoint();
        reports.MapGetFoodCostEndpoint();
        reports.MapGetShortageForecastEndpoint();
        reports.MapGetDriverEfficiencyEndpoint();
        reports.MapGetSupplierSpendEndpoint();
        reports.MapGetCancellationsEndpoint();

        return app;
    }
}
