using CateringSaaS.Modules.Knowledge.Configuration;
using CateringSaaS.Modules.Knowledge.Data;
using CateringSaaS.Modules.Knowledge.Endpoints;
using CateringSaaS.Modules.Knowledge.Services;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CateringSaaS.Modules.Knowledge;

public static class KnowledgeModuleExtensions
{
    public static IServiceCollection AddKnowledgeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ModuleConfigurationRegistry.Register(typeof(KnowledgeDocumentConfiguration).Assembly);

        services.Configure<HuggingFaceOptions>(configuration.GetSection(HuggingFaceOptions.SectionName));

        services.AddHttpClient<IEmbeddingService, HuggingFaceEmbeddingService>((sp, client) =>
        {
            var options = configuration.GetSection(HuggingFaceOptions.SectionName).Get<HuggingFaceOptions>()
                          ?? new HuggingFaceOptions();
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://router.huggingface.co"
                : options.BaseUrl.TrimEnd('/');
            client.BaseAddress = new Uri(baseUrl + "/");
            client.Timeout = TimeSpan.FromMinutes(3);
        });

        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentParser, DocumentParser>();
        services.AddScoped<KnowledgeBaseService>();
        services.AddScoped<IKnowledgeBaseService>(sp => sp.GetRequiredService<KnowledgeBaseService>());
        services.AddScoped<IKnowledgeSearchQueries>(sp => sp.GetRequiredService<KnowledgeBaseService>());

        return services;
    }

    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/knowledge")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "SuperAdmin"));

        group.MapUploadKnowledgeDocumentEndpoint();
        group.MapListKnowledgeDocumentsEndpoint();
        group.MapDeleteKnowledgeDocumentEndpoint();

        return app;
    }
}
