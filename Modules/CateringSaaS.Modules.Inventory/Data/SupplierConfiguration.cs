using CateringSaaS.Modules.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Inventory.Data;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkspaceId).IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Phone).HasMaxLength(64);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.IsActive).IsRequired();

        builder.HasIndex(s => s.WorkspaceId);
        builder.HasIndex(s => new { s.WorkspaceId, s.Name });
        builder.HasIndex(s => new { s.WorkspaceId, s.IsActive });
    }
}
