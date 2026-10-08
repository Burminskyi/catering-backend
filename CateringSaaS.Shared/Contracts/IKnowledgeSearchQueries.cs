namespace CateringSaaS.Shared.Contracts;

public sealed record KnowledgeSearchHit(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string FileName,
    int ChunkIndex,
    string Content,
    double Distance,
    string? DownloadUrl = null);

public interface IKnowledgeSearchQueries
{
    Task<IReadOnlyList<KnowledgeSearchHit>> SearchAsync(
        Guid workspaceId,
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}
