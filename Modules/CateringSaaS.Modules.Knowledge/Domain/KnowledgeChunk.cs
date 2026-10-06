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

    /// <summary>BAAI/bge-m3 embedding; mapped as PostgreSQL vector(1024).</summary>
    public Vector Embedding { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class KnowledgeEmbeddingConstants
{
    public const string DefaultModel = "BAAI/bge-m3";
    public const int Dimensions = 1024;
}
