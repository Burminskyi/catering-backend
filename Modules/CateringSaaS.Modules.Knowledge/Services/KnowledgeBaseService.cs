using CateringSaaS.Modules.Knowledge.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace CateringSaaS.Modules.Knowledge.Services;

public sealed class KnowledgeBaseService : IKnowledgeBaseService, IKnowledgeSearchQueries
{
    private readonly AppDbContext _dbContext;
    private readonly IDocumentParser _parser;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IStorageService _storage;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(
        AppDbContext dbContext,
        IDocumentParser parser,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IStorageService storage,
        ILogger<KnowledgeBaseService> logger)
    {
        _dbContext = dbContext;
        _parser = parser;
        _chunker = chunker;
        _embeddings = embeddings;
        _storage = storage;
        _logger = logger;
    }

    public async Task<KnowledgeIngestResult> IngestDocumentAsync(
        Stream fileStream,
        string fileName,
        Guid workspaceId,
        Guid? uploadedByUserId = null,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("WorkspaceId is required.", nameof(workspaceId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (!_parser.CanParse(fileName, contentType))
        {
            throw new NotSupportedException("Only PDF and DOCX files are supported.");
        }

        Stream working = fileStream;
        MemoryStream? owned = null;
        if (!fileStream.CanSeek)
        {
            owned = new MemoryStream();
            await fileStream.CopyToAsync(owned, cancellationToken);
            owned.Position = 0;
            working = owned;
        }

        var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
            ? GuessContentType(fileName)
            : contentType!;

        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = Path.GetFileNameWithoutExtension(fileName).Trim(),
            OriginalFileName = Path.GetFileName(fileName),
            ContentType = resolvedContentType,
            Status = KnowledgeDocumentStatus.Processing,
            EmbeddingModel = _embeddings.ModelName,
            EmbeddingDimensions = _embeddings.Dimensions,
            UploadedByUserId = uploadedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (string.IsNullOrWhiteSpace(document.Title))
        {
            document.Title = document.OriginalFileName;
        }

        await ReplaceExistingFileAsync(workspaceId, document.OriginalFileName, cancellationToken);

        working.Position = 0;
        document.FileUrl = await _storage.UploadFileAsync(
            working,
            document.OriginalFileName,
            resolvedContentType,
            workspaceId,
            cancellationToken);

        await _dbContext.Set<KnowledgeDocument>().AddAsync(document, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            working.Position = 0;
            var text = await _parser.ExtractTextAsync(working, fileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No extractable text found in the document.");
            }

            var chunks = _chunker.Chunk(text);
            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("Chunker produced no text segments.");
            }

            var embeddings = await _embeddings.GenerateEmbeddingsAsync(
                chunks.Select(c => c.Content).ToList(),
                cancellationToken);

            if (embeddings.Count != chunks.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding count mismatch: chunks={chunks.Count}, embeddings={embeddings.Count}.");
            }

            var entities = new List<KnowledgeChunk>(chunks.Count);
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                entities.Add(new KnowledgeChunk
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    DocumentId = document.Id,
                    ChunkIndex = chunk.Index,
                    Content = chunk.Content.Length <= 8000 ? chunk.Content : chunk.Content[..8000],
                    TokenEstimate = chunk.TokenEstimate,
                    Embedding = new Vector(embeddings[i]),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await _dbContext.Set<KnowledgeChunk>().AddRangeAsync(entities, cancellationToken);

            document.Status = KnowledgeDocumentStatus.Ready;
            document.ChunkCount = entities.Count;
            document.ProcessedAtUtc = DateTime.UtcNow;
            document.ErrorMessage = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new KnowledgeIngestResult(
                document.Id,
                document.Title,
                document.Status,
                document.ChunkCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Knowledge ingest failed for document {DocumentId}", document.Id);

            document.Status = KnowledgeDocumentStatus.Failed;
            document.ErrorMessage = ex.Message.Length <= 2000 ? ex.Message : ex.Message[..2000];
            document.ProcessedAtUtc = DateTime.UtcNow;
            document.ChunkCount = 0;

            var orphanChunks = await _dbContext.Set<KnowledgeChunk>()
                .IgnoreQueryFilters()
                .Where(c => c.DocumentId == document.Id)
                .ToListAsync(cancellationToken);
            if (orphanChunks.Count > 0)
            {
                _dbContext.Set<KnowledgeChunk>().RemoveRange(orphanChunks);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new KnowledgeIngestResult(
                document.Id,
                document.Title,
                document.Status,
                0,
                document.ErrorMessage);
        }
        finally
        {
            if (owned is not null)
            {
                await owned.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyList<KnowledgeDocumentDto>> ListDocumentsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        EnsureWorkspace(workspaceId);

        var items = await _dbContext.Set<KnowledgeDocument>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.WorkspaceId == workspaceId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items
            .Select(d => new KnowledgeDocumentDto(
                d.Id,
                d.Title,
                d.OriginalFileName,
                d.ContentType,
                ResolveDownloadUrl(d.FileUrl),
                d.Status.ToString(),
                d.ChunkCount,
                d.ErrorMessage,
                d.CreatedAtUtc,
                d.ProcessedAtUtc))
            .ToList();
    }

    private string? ResolveDownloadUrl(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return null;
        }

        try
        {
            var url = _storage.GetDownloadUrl(fileUrl);
            return string.IsNullOrWhiteSpace(url) ? null : url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not build download URL for knowledge file {FileUrl}", fileUrl);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        EnsureWorkspace(workspaceId);

        var document = await _dbContext.Set<KnowledgeDocument>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                d => d.Id == documentId && d.WorkspaceId == workspaceId,
                cancellationToken);

        if (document is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(document.FileUrl))
        {
            try
            {
                await _storage.DeleteFileAsync(document.FileUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "R2 delete failed for document {DocumentId}; continuing with DB delete",
                    document.Id);
            }
        }

        _dbContext.Set<KnowledgeDocument>().Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<KnowledgeSearchHit>> SearchAsync(
        Guid workspaceId,
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        EnsureWorkspace(workspaceId);

        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Array.Empty<KnowledgeSearchHit>();
        }

        var k = Math.Clamp(topK, 1, 10);
        var embedding = await _embeddings.GenerateEmbeddingAsync(trimmed, cancellationToken);
        var queryVector = new Vector(embedding);
        var modelName = _embeddings.ModelName;
        var isLocal = modelName.StartsWith("local/", StringComparison.OrdinalIgnoreCase);
        var maxDistance = isLocal
            ? KnowledgeEmbeddingConstants.MaxCosineDistanceLocal
            : KnowledgeEmbeddingConstants.MaxCosineDistance;

        var projected = await _dbContext.Set<KnowledgeChunk>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .Where(c => c.Document.Status == KnowledgeDocumentStatus.Ready)
            .OrderBy(c => VectorDbFunctionsExtensions.CosineDistance(c.Embedding, queryVector))
            .Take(k)
            .Select(c => new
            {
                c.Id,
                c.DocumentId,
                c.Document.Title,
                c.Document.OriginalFileName,
                c.Document.FileUrl,
                c.ChunkIndex,
                c.Content,
                Distance = VectorDbFunctionsExtensions.CosineDistance(c.Embedding, queryVector)
            })
            .ToListAsync(cancellationToken);

        var hits = projected
            .Select(c => new KnowledgeSearchHit(
                c.Id,
                c.DocumentId,
                c.Title,
                c.OriginalFileName,
                c.ChunkIndex,
                c.Content,
                c.Distance,
                ResolveDownloadUrl(c.FileUrl)))
            .ToList();

        _logger.LogInformation(
            "Knowledge vector search: model={Model}, query=\"{Query}\", topK={TopK}, maxCosineDistance={MaxDistance}, rawHits={RawCount}",
            modelName,
            trimmed.Length > 120 ? trimmed[..120] + "…" : trimmed,
            k,
            maxDistance,
            hits.Count);

        foreach (var hit in hits)
        {
            var snippet = hit.Content.Length <= 160
                ? hit.Content
                : hit.Content[..160].TrimEnd() + "…";
            var accepted = hit.Distance <= maxDistance;
            _logger.LogInformation(
                "Knowledge hit: accepted={Accepted} distance={Distance:F4} title=\"{Title}\" file=\"{File}\" chunk=#{ChunkIndex} snippet=\"{Snippet}\"",
                accepted,
                hit.Distance,
                hit.DocumentTitle,
                hit.FileName,
                hit.ChunkIndex,
                snippet);
        }

        var filtered = hits
            .Where(hit => hit.Distance <= maxDistance)
            .ToList();

        if (hits.Count > 0 && filtered.Count == 0)
        {
            _logger.LogWarning(
                "Knowledge vector search filtered out all {RawCount} chunks (best distance={Best:F4} > max={MaxDistance}). Model={Model}.",
                hits.Count,
                hits.Min(h => h.Distance),
                maxDistance,
                modelName);
        }

        return filtered;
    }

    private async Task ReplaceExistingFileAsync(
        Guid workspaceId,
        string fileName,
        CancellationToken cancellationToken)
    {
        var normalized = fileName.Trim();
        var existing = await _dbContext.Set<KnowledgeDocument>()
            .IgnoreQueryFilters()
            .Where(d => d.WorkspaceId == workspaceId)
            .Where(d => d.OriginalFileName.ToLower() == normalized.ToLower())
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        foreach (var doc in existing)
        {
            if (string.IsNullOrWhiteSpace(doc.FileUrl))
            {
                continue;
            }

            try
            {
                await _storage.DeleteFileAsync(doc.FileUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "R2 delete failed while replacing document {DocumentId}",
                    doc.Id);
            }
        }

        _dbContext.Set<KnowledgeDocument>().RemoveRange(existing);
    }

    private static void EnsureWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("WorkspaceId is required.", nameof(workspaceId));
        }
    }

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
}
