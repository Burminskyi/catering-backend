using CateringSaaS.Modules.Assistant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Assistant.Data;

public sealed class AssistantConversationConfiguration : IEntityTypeConfiguration<AssistantConversation>
{
    public void Configure(EntityTypeBuilder<AssistantConversation> builder)
    {
        builder.ToTable("assistant_conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.MessagesJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.LastArtifactsJson)
            .HasColumnType("jsonb");

        builder.HasIndex(c => new { c.WorkspaceId, c.UserId, c.UpdatedAtUtc });
    }
}
