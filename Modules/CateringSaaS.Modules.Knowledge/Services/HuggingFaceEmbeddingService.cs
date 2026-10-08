using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CateringSaaS.Modules.Knowledge.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CateringSaaS.Modules.Knowledge.Services;

/// <summary>
/// Free embedding provider via Hugging Face Router (feature-extraction).
/// Default model: BAAI/bge-m3 → 1024 dimensions.
/// </summary>
public sealed class HuggingFaceEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;
    private readonly ILogger<HuggingFaceEmbeddingService> _logger;

    public HuggingFaceEmbeddingService(
        HttpClient httpClient,
        IOptions<HuggingFaceOptions> options,
        ILogger<HuggingFaceEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
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

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "HuggingFace:ApiKey is not configured. Create a free token at https://huggingface.co/settings/tokens");
        }

        // Process in small batches to stay within free-tier payload limits.
        const int batchSize = 8;
        var results = new List<float[]>(texts.Count);

        for (var offset = 0; offset < texts.Count; offset += batchSize)
        {
            var slice = texts.Skip(offset).Take(batchSize).Select(NormalizeInput).ToArray();
            var embeddings = await RequestEmbeddingsAsync(slice, cancellationToken);
            results.AddRange(embeddings);
        }

        return results;
    }

    private async Task<IReadOnlyList<float[]>> RequestEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken)
    {
        // POST https://router.huggingface.co/hf-inference/models/BAAI/bge-m3/pipeline/feature-extraction
        var modelPath = _options.Model.Trim().Trim('/');
        var relativeUrl = $"hf-inference/models/{modelPath}/pipeline/feature-extraction";

        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            inputs,
            options = new { wait_for_model = true }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Hugging Face embedding failed ({Status}): {Body}",
                (int)response.StatusCode,
                Truncate(payload, 500));

            if ((int)response.StatusCode == 402
                || payload.Contains("no remaining credits", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Hugging Face credits are exhausted. Top up Inference Providers credits, "
                    + "or set Embeddings:Provider=OpenAI with OpenAIEmbeddings:ApiKey.");
            }

            throw new InvalidOperationException(
                $"Hugging Face embedding request failed ({(int)response.StatusCode}). {Truncate(payload, 200)}");
        }

        return ParseEmbeddings(payload, inputs.Count);
    }

    private IReadOnlyList<float[]> ParseEmbeddings(string json, int expectedCount)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // HF may return: [[...1024 floats...], ...] OR nested [[[...]]] for some models.
        var vectors = new List<float[]>();

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var first = root[0];
            if (first.ValueKind == JsonValueKind.Array
                && first.GetArrayLength() > 0
                && first[0].ValueKind == JsonValueKind.Number)
            {
                // Shape: [embedding] or [embedding, embedding, ...]
                foreach (var item in root.EnumerateArray())
                {
                    vectors.Add(ReadVector(item));
                }
            }
            else if (first.ValueKind == JsonValueKind.Array)
            {
                // Shape: [[token_vectors...]] — mean-pool token embeddings per input
                foreach (var item in root.EnumerateArray())
                {
                    vectors.Add(MeanPool(item));
                }
            }
        }

        if (vectors.Count == 0)
        {
            throw new InvalidOperationException("Hugging Face returned an unexpected embedding payload shape.");
        }

        if (vectors.Count == 1 && expectedCount > 1)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} embeddings but received {vectors.Count}.");
        }

        foreach (var vector in vectors)
        {
            if (vector.Length != Dimensions)
            {
                throw new InvalidOperationException(
                    $"Embedding dimension mismatch: expected {Dimensions}, got {vector.Length}. Check HuggingFace:Model/Dimensions.");
            }
        }

        return vectors;
    }

    private static float[] ReadVector(JsonElement array)
    {
        var values = new float[array.GetArrayLength()];
        var i = 0;
        foreach (var number in array.EnumerateArray())
        {
            values[i++] = number.GetSingle();
        }

        return values;
    }

    private static float[] MeanPool(JsonElement tokenVectors)
    {
        if (tokenVectors.GetArrayLength() == 0)
        {
            return [];
        }

        var first = tokenVectors[0];
        if (first.ValueKind != JsonValueKind.Array)
        {
            return ReadVector(tokenVectors);
        }

        var dims = first.GetArrayLength();
        var sums = new double[dims];
        var count = 0;

        foreach (var token in tokenVectors.EnumerateArray())
        {
            if (token.ValueKind != JsonValueKind.Array || token.GetArrayLength() != dims)
            {
                continue;
            }

            var i = 0;
            foreach (var number in token.EnumerateArray())
            {
                sums[i++] += number.GetDouble();
            }

            count++;
        }

        if (count == 0)
        {
            return [];
        }

        var result = new float[dims];
        for (var i = 0; i < dims; i++)
        {
            result[i] = (float)(sums[i] / count);
        }

        return result;
    }

    private static string NormalizeInput(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return " ";
        }

        // Keep payload reasonable for free tier.
        return trimmed.Length <= 8000 ? trimmed : trimmed[..8000];
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
