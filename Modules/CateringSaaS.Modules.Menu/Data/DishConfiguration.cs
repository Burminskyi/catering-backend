using CateringSaaS.Modules.Menu.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Menu.Data;

public sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        builder.ToTable("dishes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.WorkspaceId).IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(d => d.OutputWeight).IsRequired();

        builder.Property(d => d.Instructions)
            .HasMaxLength(8000);

        builder.Property(d => d.IsActive).IsRequired();

        builder.HasIndex(d => d.WorkspaceId);
        builder.HasIndex(d => new { d.WorkspaceId, d.Name });
        builder.HasIndex(d => new { d.WorkspaceId, d.IsActive });

        builder.HasMany(d => d.Ingredients)
            .WithOne(i => i.Dish)
            .HasForeignKey(i => i.DishId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
