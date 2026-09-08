using CateringSaaS.Modules.Menu.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CateringSaaS.Modules.Menu.Data;

/// <summary>
/// Persists canonical category codes (Side, Drink) while reading legacy Beverage/Bakery rows.
/// </summary>
public sealed class DishCategoryValueConverter : ValueConverter<DishCategory, string>
{
    public DishCategoryValueConverter()
        : base(
            category => ToStorage(category),
            value => FromStorage(value))
    {
    }

    public static string ToStorage(DishCategory category) => category.ToString();

    public static DishCategory FromStorage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DishCategory.Main;
        }

        if (Enum.TryParse<DishCategory>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "beverage" => DishCategory.Drink,
            "bakery" => DishCategory.Side,
            _ => DishCategory.Main
        };
    }
}
