using CateringSaaS.Modules.Knowledge.Configuration;
using CateringSaaS.Modules.Knowledge.Data;
using CateringSaaS.Modules.Knowledge.Endpoints;
using CateringSaaS.Modules.Knowledge.Http;
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
    // Fixed absolute URL — do not take BaseAddress from env/config.
    // Invalid HuggingFace__BaseUrl (markdown links, missing scheme) used to throw
    // UriFormatException while resolving IEmbeddingService and break every assistant request.
    private const string HuggingFaceRouterBaseUrl = "https://router.huggingface.co/";

    public static IServiceCollection AddKnowledgeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ModuleConfigurationRegistry.Register(typeof(KnowledgeDocumentConfiguration).Assembly);

        services.Configure<HuggingFaceOptions>(configuration.GetSection(HuggingFaceOptions.SectionName));
        services.Configure<OpenAiEmbeddingOptions>(configuration.GetSection(OpenAiEmbeddingOptions.SectionName));
        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        var embeddingProvider = configuration
            .GetSection(EmbeddingOptions.SectionName)
            .GetValue<string>("Provider")
            ?.Trim();

        if (string.Equals(embeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        }
        else if (string.Equals(embeddingProvider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            // Free CPU ONNX embeddings — no Hugging Face / OpenAI credits required.
            services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();
        }
        else
        {
            services.AddHttpClient<IEmbeddingService, HuggingFaceEmbeddingService>(client =>
            {
                client.BaseAddress = new Uri(HuggingFaceRouterBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(3);
            })
            .AddPolicyHandler(TransientHttpRetryPolicy.Create());
        }

        services.AddSingleton<IStorageService, S3StorageService>();
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
