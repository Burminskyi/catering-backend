namespace CateringSaaS.Modules.Knowledge.Domain;

public class KnowledgeDocument
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public required string Title { get; set; }

    public required string OriginalFileName { get; set; }

    public required string ContentType { get; set; }

    /// <summary>R2/S3 object key (or URL) for the original uploaded file.</summary>
    public string? FileUrl { get; set; }

    public KnowledgeDocumentStatus Status { get; set; } = KnowledgeDocumentStatus.Pending;

    public string? ErrorMessage { get; set; }

    public string EmbeddingModel { get; set; } = KnowledgeEmbeddingConstants.DefaultModel;

    public int EmbeddingDimensions { get; set; } = KnowledgeEmbeddingConstants.Dimensions;

    public int ChunkCount { get; set; }

    public Guid? UploadedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }

    public ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
}
