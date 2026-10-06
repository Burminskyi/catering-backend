using CateringSaaS.Modules.Assistant.Configuration;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Endpoints;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Assistant.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Assistant;

public static class AssistantModuleExtensions
{
    public static IServiceCollection AddAssistantModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));

        services.AddSingleton<IAssistantConversationStore, InMemoryAssistantConversationStore>();
        services.AddScoped<IAssistantChatService, AssistantChatService>();

        services.AddScoped<IAssistantTool, ResolveClientTool>();
        services.AddScoped<IAssistantTool, GetOperationsPulseTool>();
        services.AddScoped<IAssistantTool, GetRevenueByClientTool>();
        services.AddScoped<IAssistantTool, SearchStockBalanceTool>();
        services.AddScoped<IAssistantTool, GetDeliveryAndReclamationsTool>();
        services.AddScoped<IAssistantTool, GetShortageForecastTool>();
        services.AddScoped<IAssistantTool, GetReclamationHeatmapTool>();
        services.AddScoped<IAssistantTool, GetConsumptionVarianceTool>();
        services.AddScoped<IAssistantTool, GetFoodCostTool>();
        services.AddScoped<IAssistantTool, GetDriverEfficiencyTool>();
        services.AddScoped<IAssistantTool, GetSupplierSpendTool>();
        services.AddScoped<IAssistantTool, GetCancellationsTool>();

        return services;
    }

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assistant")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "SuperAdmin"));

        group.MapChatEndpoint();

        return app;
    }
}
