using CateringSaaS.Modules.Knowledge.Domain;

namespace CateringSaaS.Modules.Knowledge.Services;

public sealed record KnowledgeIngestResult(
    Guid DocumentId,
    string Title,
    KnowledgeDocumentStatus Status,
    int ChunkCount,
    string? ErrorMessage = null);

public sealed record KnowledgeDocumentDto(
    Guid Id,
    string Title,
    string OriginalFileName,
    string ContentType,
    string Status,
    int ChunkCount,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc);

public interface IKnowledgeBaseService
{
    Task<KnowledgeIngestResult> IngestDocumentAsync(
        Stream fileStream,
        string fileName,
        Guid workspaceId,
        Guid? uploadedByUserId = null,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeDocumentDto>> ListDocumentsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
        Guid documentId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
