using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.Modules.Inventory.Data;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<InventoryEntity>
{
    public void Configure(EntityTypeBuilder<InventoryEntity> builder)
    {
        builder.ToTable("inventories");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WorkspaceId)
            .IsRequired();

        builder.Property(i => i.IngredientId)
            .IsRequired();

        builder.Property(i => i.TotalQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasIndex(i => new { i.WorkspaceId, i.IngredientId })
            .IsUnique();

        // Workspace is owned by Tenants module — store FK id only (no cross-module navigation).
        builder.HasIndex(i => i.WorkspaceId);
    }
}
