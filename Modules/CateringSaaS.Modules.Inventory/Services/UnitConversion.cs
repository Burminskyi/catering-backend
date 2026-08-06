using CateringSaaS.Modules.Inventory.Domain.Enums;

namespace CateringSaaS.Modules.Inventory.Services;

public static class UnitConversion
{
    /// <summary>
    /// Converts a UI quantity (kg, liters, pieces, g, ml, ...) into the ingredient's base unit quantity.
    /// </summary>
    public static decimal ToBaseUnits(decimal quantity, string uiUnit, UnitOfMeasure baseUnit)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var unit = Normalize(uiUnit);

        return baseUnit switch
        {
            UnitOfMeasure.Gram => ConvertToGrams(quantity, unit),
            UnitOfMeasure.Milliliter => ConvertToMilliliters(quantity, unit),
            UnitOfMeasure.Piece => ConvertToPieces(quantity, unit),
            _ => throw new ArgumentOutOfRangeException(nameof(baseUnit), baseUnit, "Unsupported base unit.")
        };
    }

    public static decimal FromBaseUnits(decimal baseQuantity, string uiUnit, UnitOfMeasure baseUnit)
    {
        var unit = Normalize(uiUnit);
        var factor = GetFactorToBase(unit, baseUnit);
        return baseQuantity / factor;
    }

    private static decimal ConvertToGrams(decimal quantity, string unit) =>
        quantity * unit switch
        {
            "g" or "gram" or "grams" => 1m,
            "kg" or "kilogram" or "kilograms" => 1000m,
            _ => throw Unsupported(unit, UnitOfMeasure.Gram)
        };

    private static decimal ConvertToMilliliters(decimal quantity, string unit) =>
        quantity * unit switch
        {
            "ml" or "milliliter" or "milliliters" => 1m,
            "l" or "liter" or "liters" or "litre" or "litres" => 1000m,
            _ => throw Unsupported(unit, UnitOfMeasure.Milliliter)
        };

    private static decimal ConvertToPieces(decimal quantity, string unit) =>
        unit switch
        {
            "pc" or "pcs" or "piece" or "pieces" => quantity,
            _ => throw Unsupported(unit, UnitOfMeasure.Piece)
        };

    private static decimal GetFactorToBase(string unit, UnitOfMeasure baseUnit) =>
        baseUnit switch
        {
            UnitOfMeasure.Gram => unit switch
            {
                "g" or "gram" or "grams" => 1m,
                "kg" or "kilogram" or "kilograms" => 1000m,
                _ => throw Unsupported(unit, baseUnit)
            },
            UnitOfMeasure.Milliliter => unit switch
            {
                "ml" or "milliliter" or "milliliters" => 1m,
                "l" or "liter" or "liters" or "litre" or "litres" => 1000m,
                _ => throw Unsupported(unit, baseUnit)
            },
            UnitOfMeasure.Piece => unit switch
            {
                "pc" or "pcs" or "piece" or "pieces" => 1m,
                _ => throw Unsupported(unit, baseUnit)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(baseUnit))
        };

    private static string Normalize(string uiUnit) =>
        (uiUnit ?? string.Empty).Trim().ToLowerInvariant();

    private static InvalidOperationException Unsupported(string unit, UnitOfMeasure baseUnit) =>
        new($"Unit '{unit}' is not compatible with base unit '{baseUnit}'.");
}
