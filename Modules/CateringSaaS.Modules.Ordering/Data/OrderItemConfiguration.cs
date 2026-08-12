using CateringSaaS.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Ordering.Data;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WorkspaceId).IsRequired();
        builder.Property(i => i.OrderId).IsRequired();
        builder.Property(i => i.MenuItemId).IsRequired();

        builder.Property(i => i.Quantity).IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(i => i.WorkspaceId);
        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.MenuItemId);
    }
}
