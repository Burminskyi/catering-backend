using CateringSaaS.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Ordering.Data;

public sealed class EmployeeMealRequestItemConfiguration : IEntityTypeConfiguration<EmployeeMealRequestItem>
{
    public void Configure(EntityTypeBuilder<EmployeeMealRequestItem> builder)
    {
        builder.ToTable("employee_meal_request_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WorkspaceId).IsRequired();
        builder.Property(i => i.RequestId).IsRequired();
        builder.Property(i => i.MenuItemId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(i => i.WorkspaceId);
        builder.HasIndex(i => i.RequestId);
        builder.HasIndex(i => i.MenuItemId);
    }
}
