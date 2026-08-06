using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Inventory.Data;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(i => i.BaseUnit)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(i => i.WorkspaceId);

        // Supporting lookup for uniqueness validation (global + per-workspace).
        builder.HasIndex(i => new { i.WorkspaceId, i.Name });

        builder.HasMany(i => i.StockBatches)
            .WithOne(b => b.Ingredient)
            .HasForeignKey(b => b.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Inventories)
            .WithOne(inv => inv.Ingredient)
            .HasForeignKey(inv => inv.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
