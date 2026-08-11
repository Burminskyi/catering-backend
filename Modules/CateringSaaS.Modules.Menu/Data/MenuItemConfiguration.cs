using CateringSaaS.Modules.Menu.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Menu.Data;

public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WorkspaceId).IsRequired();
        builder.Property(i => i.MenuDayId).IsRequired();
        builder.Property(i => i.DishId).IsRequired();

        builder.Property(i => i.SellingPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(i => i.WorkspaceId);
        builder.HasIndex(i => i.MenuDayId);
        builder.HasIndex(i => new { i.MenuDayId, i.DishId }).IsUnique();

        builder.HasOne(i => i.Dish)
            .WithMany()
            .HasForeignKey(i => i.DishId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
