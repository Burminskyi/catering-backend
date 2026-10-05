namespace CateringSaaS.Shared.Reporting;

/// <summary>
/// Unified report envelope for UI dashboards and future assistant tools.
/// </summary>
public sealed record ReportResponse(
    string Key,
    string Title,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    IReadOnlyList<ReportMetric> Metrics,
    IReadOnlyList<ReportTable> Tables,
    IReadOnlyList<ReportSeries> Series);

public sealed record ReportMetric(
    string Key,
    string Label,
    decimal? NumericValue,
    string DisplayValue,
    string? Note = null,
    string? Trend = null);

public sealed record ReportColumn(
    string Key,
    string Label,
    string Kind = "text");

public sealed record ReportTable(
    string Key,
    string Title,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public sealed record ReportSeriesPoint(
    string Label,
    decimal Value,
    string? Group = null);

/// <param name="Chart">bar | line | pie</param>
public sealed record ReportSeries(
    string Key,
    string Label,
    string Chart,
    IReadOnlyList<ReportSeriesPoint> Points);
