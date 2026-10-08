using System.ClientModel;
using CateringSaaS.Modules.Knowledge.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace CateringSaaS.Modules.Knowledge.Services;

/// <summary>
/// OpenAI embeddings with fixed dimensions to match pgvector(1024).
/// </summary>
public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly OpenAiEmbeddingOptions _options;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(
        IOptions<OpenAiEmbeddingOptions> options,
        ILogger<OpenAiEmbeddingService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAIEmbeddings:ApiKey is not configured.");
        }

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl)
            && Uri.TryCreate(_options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            clientOptions.Endpoint = baseUri;
        }

        var openAi = new OpenAIClient(new ApiKeyCredential(_options.ApiKey), clientOptions);
        _client = openAi.GetEmbeddingClient(_options.Model);
    }

    public int Dimensions => _options.Dimensions;

    public string ModelName => _options.Model;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var batch = await GenerateEmbeddingsAsync([text], cancellationToken);
        return batch[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        const int batchSize = 64;
        var results = new List<float[]>(texts.Count);

        for (var offset = 0; offset < texts.Count; offset += batchSize)
        {
            var slice = texts
                .Skip(offset)
                .Take(batchSize)
                .Select(NormalizeInput)
                .ToArray();

            try
            {
                var response = await _client.GenerateEmbeddingsAsync(
                    slice,
                    new EmbeddingGenerationOptions { Dimensions = Dimensions },
                    cancellationToken);

                var ordered = response.Value
                    .OrderBy(item => item.Index)
                    .Select(item =>
                    {
                        var vector = item.ToFloats().ToArray();
                        if (vector.Length != Dimensions)
                        {
                            throw new InvalidOperationException(
                                $"Embedding dimension mismatch: expected {Dimensions}, got {vector.Length}.");
                        }

                        return vector;
                    })
                    .ToList();

                if (ordered.Count != slice.Length)
                {
                    throw new InvalidOperationException(
                        $"Expected {slice.Length} embeddings but received {ordered.Count}.");
                }

                results.AddRange(ordered);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "OpenAI embedding request failed");
                throw new InvalidOperationException(
                    $"OpenAI embedding request failed: {ex.Message}", ex);
            }
        }

        return results;
    }

    private static string NormalizeInput(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return " ";
        }

        return trimmed.Length <= 8000 ? trimmed : trimmed[..8000];
    }
}
