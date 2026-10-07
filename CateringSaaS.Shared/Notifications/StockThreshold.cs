namespace CateringSaaS.Shared.Notifications;

/// <summary>
/// Shared low-stock thresholds (base units). Keep in sync with inventory reporting.
/// </summary>
public static class StockThreshold
{
    public static decimal ForUnit(string baseUnit) =>
        baseUnit.ToLowerInvariant() switch
        {
            "milliliter" or "ml" => 10_000m,
            "piece" or "pcs" or "pc" => 20m,
            _ => 6_000m
        };

    public static decimal ForUnitEnum(int unitOrdinal) =>
        unitOrdinal switch
        {
            1 => 10_000m, // Milliliter
            2 => 20m,     // Piece
            _ => 6_000m   // Gram / default
        };
}
