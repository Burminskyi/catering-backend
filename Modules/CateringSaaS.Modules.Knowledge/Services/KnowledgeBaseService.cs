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
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(
        AppDbContext dbContext,
        IDocumentParser parser,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        ILogger<KnowledgeBaseService> logger)
    {
        _dbContext = dbContext;
        _parser = parser;
        _chunker = chunker;
        _embeddings = embeddings;
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

        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = Path.GetFileNameWithoutExtension(fileName).Trim(),
            OriginalFileName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? GuessContentType(fileName)
                : contentType!,
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
            .Select(d => new KnowledgeDocumentDto(
                d.Id,
                d.Title,
                d.OriginalFileName,
                d.ContentType,
                d.Status.ToString(),
                d.ChunkCount,
                d.ErrorMessage,
                d.CreatedAtUtc,
                d.ProcessedAtUtc))
            .ToListAsync(cancellationToken);

        return items;
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

        var hits = await _dbContext.Set<KnowledgeChunk>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .Where(c => c.Document.Status == KnowledgeDocumentStatus.Ready)
            .OrderBy(c => VectorDbFunctionsExtensions.CosineDistance(c.Embedding, queryVector))
            .Take(k)
            .Select(c => new KnowledgeSearchHit(
                c.Id,
                c.DocumentId,
                c.Document.Title,
                c.Document.OriginalFileName,
                c.ChunkIndex,
                c.Content,
                VectorDbFunctionsExtensions.CosineDistance(c.Embedding, queryVector)))
            .ToListAsync(cancellationToken);

        return hits
            .Where(hit => hit.Distance <= KnowledgeEmbeddingConstants.MaxCosineDistance)
            .ToList();
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
