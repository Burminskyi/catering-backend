using CateringSaaS.Modules.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Inventory.Data;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.WorkspaceId).IsRequired();
        builder.Property(m => m.IngredientId).IsRequired();

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.SignedQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.TotalCost)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.Source)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Reason).HasMaxLength(1000);
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.WorkspaceId);
        builder.HasIndex(m => new { m.WorkspaceId, m.CreatedAt });
        builder.HasIndex(m => new { m.WorkspaceId, m.IngredientId, m.CreatedAt });
        builder.HasIndex(m => new { m.WorkspaceId, m.Type, m.CreatedAt });

        builder.HasOne(m => m.Ingredient)
            .WithMany()
            .HasForeignKey(m => m.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
