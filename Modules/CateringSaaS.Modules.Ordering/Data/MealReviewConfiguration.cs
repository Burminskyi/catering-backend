using CateringSaaS.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Ordering.Data;

public sealed class MealReviewConfiguration : IEntityTypeConfiguration<MealReview>
{
    public void Configure(EntityTypeBuilder<MealReview> builder)
    {
        builder.ToTable("meal_reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.WorkspaceId).IsRequired();
        builder.Property(r => r.ClientCompanyId).IsRequired();
        builder.Property(r => r.EmployeeId).IsRequired();
        builder.Property(r => r.TargetDate).IsRequired();
        builder.Property(r => r.MenuItemId).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.IsReclamation).IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(2000);

        builder.Property(r => r.PhotoUrl)
            .HasMaxLength(2048);

        // No EF FKs to User/Menu/Order — Restrict by design so historical feedback survives.
        builder.HasIndex(r => r.WorkspaceId);
        builder.HasIndex(r => new { r.WorkspaceId, r.ClientCompanyId, r.TargetDate });
        builder.HasIndex(r => new { r.WorkspaceId, r.EmployeeId, r.TargetDate, r.MenuItemId })
            .IsUnique();
        builder.HasIndex(r => new { r.WorkspaceId, r.IsReclamation, r.CreatedAt });
        builder.HasIndex(r => r.MenuItemId);
    }
}
