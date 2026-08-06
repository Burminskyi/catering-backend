using CateringSaaS.Modules.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Inventory.Data;

public sealed class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.ToTable("stock_batches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.WorkspaceId)
            .IsRequired();

        builder.Property(b => b.IngredientId)
            .IsRequired();

        builder.Property(b => b.InitialQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(b => b.CurrentQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        // Total cost of the entire batch (not per-unit).
        builder.Property(b => b.CostPrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(b => b.ReceivedAt)
            .IsRequired();

        builder.HasIndex(b => new { b.WorkspaceId, b.IngredientId, b.ReceivedAt });

        // Workspace is owned by Tenants module — store FK id only (no cross-module navigation).
        builder.HasIndex(b => b.WorkspaceId);
    }
}
