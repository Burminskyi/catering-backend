using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Shared.Notifications;

public sealed class WorkspaceNotificationConfiguration : IEntityTypeConfiguration<WorkspaceNotification>
{
    public void Configure(EntityTypeBuilder<WorkspaceNotification> builder)
    {
        builder.ToTable("workspace_notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasMaxLength(64).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.LinkPath).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Audience).HasMaxLength(32).IsRequired();
        builder.Property(n => n.TargetUserId);
        builder.Property(n => n.TargetClientCompanyId);
        builder.Property(n => n.RelatedDate);

        builder.HasIndex(n => new { n.WorkspaceId, n.CreatedAtUtc });
        builder.HasIndex(n => new { n.WorkspaceId, n.Type, n.RelatedEntityId });
        builder.HasIndex(n => new { n.WorkspaceId, n.Audience, n.TargetUserId });
        builder.HasIndex(n => new
            {
                n.WorkspaceId,
                n.Type,
                n.Audience,
                n.TargetClientCompanyId,
                n.RelatedDate
            });
    }
}

public sealed class WorkspaceNotificationReadConfiguration : IEntityTypeConfiguration<WorkspaceNotificationRead>
{
    public void Configure(EntityTypeBuilder<WorkspaceNotificationRead> builder)
    {
        builder.ToTable("workspace_notification_reads");
        builder.HasKey(r => new { r.NotificationId, r.UserId });

        builder.HasOne(r => r.Notification)
            .WithMany()
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.UserId);
    }
}
