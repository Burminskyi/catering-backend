using CateringSaaS.Shared.Reporting;

namespace CateringSaaS.Modules.Reporting.Services;

internal static class ReportComposer
{
    public static ReportMetric Metric(
        string key,
        string label,
        decimal? numeric,
        string display,
        string? note = null,
        string? trend = null) =>
        new(key, label, numeric, display, note, trend);

    public static Dictionary<string, object?> Row(params (string Key, object? Value)[] pairs)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
        {
            row[key] = value;
        }

        return row;
    }

    public static string Money(decimal value) => value.ToString("0.##");

    public static string Quantity(decimal value) => value.ToString("0.##");

    public static string Percent(decimal value) => value.ToString("0.#") + "%";
}
