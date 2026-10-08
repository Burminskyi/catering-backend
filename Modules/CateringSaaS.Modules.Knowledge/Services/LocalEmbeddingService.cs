using SmartComponents.LocalEmbeddings;

namespace CateringSaaS.Modules.Knowledge.Services;

/// <summary>
/// Free, offline embeddings via ONNX (bge-micro-v2, 384 dims).
/// Values are zero-padded to <see cref="Domain.KnowledgeEmbeddingConstants.Dimensions"/> (1024)
/// so the existing pgvector schema stays unchanged.
/// </summary>
public sealed class LocalEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly LocalEmbedder _embedder = new();

    public int Dimensions => Domain.KnowledgeEmbeddingConstants.Dimensions;

    public string ModelName => "local/bge-micro-v2";

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(EmbedOne(text));
    }

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<float[]>>(Array.Empty<float[]>());
        }

        var results = new float[texts.Count][];
        for (var i = 0; i < texts.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results[i] = EmbedOne(texts[i]);
        }

        return Task.FromResult<IReadOnlyList<float[]>>(results);
    }

    private float[] EmbedOne(string text)
    {
        var normalized = string.IsNullOrWhiteSpace(text) ? " " : text.Trim();
        if (normalized.Length > 8000)
        {
            normalized = normalized[..8000];
        }

        var embedding = _embedder.Embed<EmbeddingF32>(normalized);
        var values = embedding.Values.Span;
        var output = new float[Dimensions];

        var copy = Math.Min(values.Length, Dimensions);
        values[..copy].CopyTo(output.AsSpan(0, copy));
        // Remaining dimensions stay 0 — preserves cosine similarity vs other local vectors.
        return output;
    }

    public void Dispose() => _embedder.Dispose();
}
