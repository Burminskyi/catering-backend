using CateringSaaS.Modules.Menu.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Menu.Data;

public sealed class MenuDayConfiguration : IEntityTypeConfiguration<MenuDay>
{
    public void Configure(EntityTypeBuilder<MenuDay> builder)
    {
        builder.ToTable("menu_days");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.WorkspaceId).IsRequired();
        builder.Property(d => d.MenuId).IsRequired();
        builder.Property(d => d.Date).IsRequired();

        builder.HasIndex(d => d.WorkspaceId);
        builder.HasIndex(d => new { d.MenuId, d.Date }).IsUnique();

        builder.HasMany(d => d.Items)
            .WithOne(i => i.MenuDay)
            .HasForeignKey(i => i.MenuDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
