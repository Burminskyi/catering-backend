using CateringSaaS.Modules.Menu.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MenuEntity = CateringSaaS.Modules.Menu.Domain.Menu;

namespace CateringSaaS.Modules.Menu.Data;

public sealed class MenuConfiguration : IEntityTypeConfiguration<MenuEntity>
{
    public void Configure(EntityTypeBuilder<MenuEntity> builder)
    {
        builder.ToTable("menus");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.WorkspaceId).IsRequired();

        builder.Property(m => m.ClientCompanyId);

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.StartDate).IsRequired();
        builder.Property(m => m.EndDate).IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(m => m.WorkspaceId);
        builder.HasIndex(m => new { m.WorkspaceId, m.ClientCompanyId });
        builder.HasIndex(m => new { m.WorkspaceId, m.Status });

        builder.HasMany(m => m.Days)
            .WithOne(d => d.Menu)
            .HasForeignKey(d => d.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
