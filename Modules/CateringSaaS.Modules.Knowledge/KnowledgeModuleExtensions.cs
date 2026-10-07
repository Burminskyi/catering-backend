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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CateringSaaS.Modules.Knowledge;

public static class KnowledgeModuleExtensions
{
    private const string DefaultHuggingFaceBaseUrl = "https://router.huggingface.co/";

    public static IServiceCollection AddKnowledgeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ModuleConfigurationRegistry.Register(typeof(KnowledgeDocumentConfiguration).Assembly);

        services.Configure<HuggingFaceOptions>(configuration.GetSection(HuggingFaceOptions.SectionName));

        services.AddHttpClient<IEmbeddingService, HuggingFaceEmbeddingService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CateringSaaS.Knowledge.Http");
            client.BaseAddress = ResolveHuggingFaceBaseAddress(options.BaseUrl, logger);
            client.Timeout = TimeSpan.FromMinutes(3);
        })
        .AddPolicyHandler(TransientHttpRetryPolicy.Create());

        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentParser, DocumentParser>();
        services.AddScoped<KnowledgeBaseService>();
        services.AddScoped<IKnowledgeBaseService>(sp => sp.GetRequiredService<KnowledgeBaseService>());
        services.AddScoped<IKnowledgeSearchQueries>(sp => sp.GetRequiredService<KnowledgeBaseService>());

        return services;
    }

    /// <summary>
    /// Render/env typos (missing scheme, quotes) used to throw UriFormatException and break
    /// every assistant request because SearchKnowledgeBaseTool resolves IEmbeddingService.
    /// </summary>
    internal static Uri ResolveHuggingFaceBaseAddress(string? baseUrl, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new Uri(DefaultHuggingFaceBaseUrl);
        }

        var trimmed = baseUrl.Trim().Trim('"', '\'');

        // Paste from markdown: [https://host](https://host) or <https://host>
        if (trimmed.StartsWith('[') && trimmed.Contains("](", StringComparison.Ordinal))
        {
            var close = trimmed.IndexOf(']');
            if (close > 1)
            {
                trimmed = trimmed[1..close];
            }
        }
        else if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            trimmed = trimmed[1..^1].Trim();
        }

        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed.TrimStart('/');
        }

        trimmed = trimmed.TrimEnd('/') + "/";

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri;
        }

        logger?.LogWarning(
            "Invalid HuggingFace:BaseUrl '{BaseUrl}'. Falling back to {Fallback}.",
            baseUrl,
            DefaultHuggingFaceBaseUrl);

        return new Uri(DefaultHuggingFaceBaseUrl);
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
