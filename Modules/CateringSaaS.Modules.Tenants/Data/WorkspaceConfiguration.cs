using CateringSaaS.Modules.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Tenants.Data;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Subdomain)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(w => w.Subdomain)
            .IsUnique();

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.IsActive)
            .IsRequired();

        builder.Property(w => w.SubscriptionExpiresAt)
            .IsRequired();

        builder.Property(w => w.PlanType)
            .HasMaxLength(50)
            .IsRequired();
    }
}
