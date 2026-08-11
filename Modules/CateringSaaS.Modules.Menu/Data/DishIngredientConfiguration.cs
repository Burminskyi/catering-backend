using CateringSaaS.Modules.Menu.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Menu.Data;

public sealed class DishIngredientConfiguration : IEntityTypeConfiguration<DishIngredient>
{
    public void Configure(EntityTypeBuilder<DishIngredient> builder)
    {
        builder.ToTable("dish_ingredients");

        builder.HasKey(di => di.Id);

        builder.Property(di => di.WorkspaceId).IsRequired();
        builder.Property(di => di.DishId).IsRequired();
        builder.Property(di => di.IngredientId).IsRequired();

        builder.Property(di => di.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasIndex(di => di.WorkspaceId);
        builder.HasIndex(di => di.DishId);
        builder.HasIndex(di => new { di.DishId, di.IngredientId }).IsUnique();
    }
}
