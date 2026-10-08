using Pgvector;

namespace CateringSaaS.Modules.Knowledge.Domain;

public class KnowledgeChunk
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid DocumentId { get; set; }

    public KnowledgeDocument Document { get; set; } = null!;

    public int ChunkIndex { get; set; }

    public required string Content { get; set; }

    public int TokenEstimate { get; set; }

    /// <summary>Dense embedding; mapped as PostgreSQL vector(1024). Local provider zero-pads 384→1024.</summary>
    public Vector Embedding { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class KnowledgeEmbeddingConstants
{
    public const string DefaultModel = "local/bge-micro-v2";
    public const int Dimensions = 1024;

    /// <summary>
    /// Cosine distance cutoff for multilingual models (e.g. BAAI/bge-m3).
    /// Distance 0.45 ≈ cosine similarity 0.55 — relaxed for UA/EN query → RU docs.
    /// </summary>
    public const double MaxCosineDistance = 0.45;

    /// <summary>
    /// Local ONNX (bge-micro-v2) is English-centric; cross-lingual distances run higher.
    /// </summary>
    public const double MaxCosineDistanceLocal = 0.70;
}
