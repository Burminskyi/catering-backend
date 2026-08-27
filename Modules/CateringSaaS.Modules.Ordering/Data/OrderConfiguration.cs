using CateringSaaS.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Ordering.Data;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.WorkspaceId).IsRequired();
        builder.Property(o => o.ClientCompanyId).IsRequired();
        builder.Property(o => o.PlacedByUserId).IsRequired();
        builder.Property(o => o.TargetDate).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(o => o.WorkspaceId);
        builder.HasIndex(o => new { o.WorkspaceId, o.ClientCompanyId, o.TargetDate });
        builder.HasIndex(o => new { o.WorkspaceId, o.Status });
        builder.HasIndex(o => new { o.WorkspaceId, o.PlacedByUserId });
        builder.HasIndex(o => o.PlacedByUserId);
        builder.HasIndex(o => new { o.WorkspaceId, o.DriverId, o.TargetDate, o.Status });
        builder.HasIndex(o => o.DriverId);

        // DriverId is a Guid reference only (no cross-module FK) so driver user deletes
        // do not cascade onto historical orders.
        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

