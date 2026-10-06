using CateringSaaS.Modules.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector.EntityFrameworkCore;

namespace CateringSaaS.Modules.Knowledge.Data;

public sealed class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> builder)
    {
        builder.ToTable("knowledge_chunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(c => c.Embedding)
            .HasColumnType($"vector({KnowledgeEmbeddingConstants.Dimensions})")
            .IsRequired();

        builder.HasIndex(c => new { c.WorkspaceId, c.DocumentId, c.ChunkIndex })
            .IsUnique();

        // Approximate nearest-neighbor index for cosine similarity searches.
        builder.HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
