using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Shared.Reporting;

namespace CateringSaaS.Modules.Assistant.Services;

internal static class ReportArtifactMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>At or below this size the LLM receives every row. Above it, only aggregates.</summary>
    public const int FullRowThreshold = 20;

    private const int HighlightCount = 3;

    private const string AnswerFrom =
        "Answer only from metrics and table facts. When complete is true, rows are the full set. " +
        "When complete is false, cite aggregates and highlights only; do not invent or list rows that are not in highlights. " +
        "rowCount is the size of the UI table, which already shows every row.";

    public static ToolResult FromReport(ReportResponse report, string? period = null)
    {
        var artifacts = new List<AssistantArtifact>();

        foreach (var metric in report.Metrics.Take(12))
        {
            artifacts.Add(new MetricArtifact(metric.Label, metric.DisplayValue, report.Title));
        }

        foreach (var table in report.Tables.Take(6))
        {
            artifacts.Add(new TableArtifact(
                table.Title,
                table.Columns.Select(c => new ArtifactColumn(c.Key, c.Label)).ToList(),
                table.Rows.Select(r => r.ToDictionary(kv => kv.Key, kv => kv.Value)).ToList()));
        }

        var facts = new
        {
            answerFrom = AnswerFrom,
            period,
            title = report.Title,
            dateFrom = report.DateFrom?.ToString("yyyy-MM-dd"),
            dateTo = report.DateTo?.ToString("yyyy-MM-dd"),
            metrics = report.Metrics.Select(m => new
            {
                label = m.Label,
                value = m.DisplayValue,
                note = m.Note
            }),
            tables = report.Tables.Take(6).Select(BuildTableFact)
        };

        return new ToolResult(facts, artifacts);
    }

    public static object BuildRowsFact(
        string title,
        IReadOnlyList<ArtifactColumn> columns,
        IEnumerable<IReadOnlyDictionary<string, object?>> rows) =>
        BuildTableFact(title, columns, rows.ToList());

    /// <summary>
    /// Soft ceiling for oversized report tools. Knowledge RAG needs full chunk text;
    /// after search_knowledge_base we drop tool schemas on the next turn for TPM headroom.
    /// </summary>
    public const int MaxToolJsonChars = 16_000;

    public static string ToToolJson(ToolResult result)
    {
        var json = JsonSerializer.Serialize(result.Data, JsonOptions);
        return json.Length <= MaxToolJsonChars
            ? json
            : json[..(MaxToolJsonChars - 1)] + "…";
    }

    private static object BuildTableFact(ReportTable table) =>
        BuildTableFact(
            table.Title,
            table.Columns.Select(c => new ArtifactColumn(c.Key, c.Label)).ToList(),
            table.Rows);

    private static object BuildTableFact(
        string title,
        IReadOnlyList<ArtifactColumn> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var visible = columns.Where(c => !IsIdKey(c.Key)).ToList();
        if (visible.Count == 0)
        {
            visible = columns.ToList();
        }

        var keys = visible.Select(c => c.Key).ToList();
        var labelKey = PickLabelKey(keys, rows);
        var numericKeys = keys.Where(key => IsMostlyNumeric(rows, key)).ToList();
        var columnMeta = visible.Select(c => new { key = c.Key, label = c.Label });

        if (rows.Count <= FullRowThreshold)
        {
            return new
            {
                title,
                rowCount = rows.Count,
                complete = true,
                columns = columnMeta,
                rows = rows.Select(row => Project(row, keys)).ToList()
            };
        }

        var sortKey = numericKeys.FirstOrDefault() ?? keys.FirstOrDefault();
        return new
        {
            title,
            rowCount = rows.Count,
            complete = false,
            columns = columnMeta,
            aggregates = numericKeys.Select(key => Aggregate(rows, key, labelKey)).ToList(),
            categories = keys
                .Where(key => key != labelKey && !numericKeys.Contains(key))
                .Select(key => Categorize(rows, key))
                .Where(group => group is not null)
                .ToList(),
            highlights = sortKey is null ? [] : Highlights(rows, keys, sortKey)
        };
    }

    private static Dictionary<string, object?> Project(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> keys)
    {
        var projected = new Dictionary<string, object?>(keys.Count);
        foreach (var key in keys)
        {
            projected[key] = row.TryGetValue(key, out var value) ? value : null;
        }

        return projected;
    }

    private static object Aggregate(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        string key,
        string? labelKey)
    {
        decimal? min = null;
        decimal? max = null;
        decimal sum = 0;
        var count = 0;
        string? minLabel = null;
        string? maxLabel = null;

        foreach (var row in rows)
        {
            if (!TryNumber(Read(row, key), out var value))
            {
                continue;
            }

            count++;
            sum += value;
            var label = labelKey is null ? null : Read(row, labelKey)?.ToString();
            if (min is null || value < min)
            {
                min = value;
                minLabel = label;
            }

            if (max is null || value > max)
            {
                max = value;
                maxLabel = label;
            }
        }

        return new
        {
            key,
            count,
            sum,
            min,
            minLabel,
            max,
            maxLabel
        };
    }

    private static object? Categorize(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, string key)
    {
        var groups = rows
            .Select(row => Read(row, key)?.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ToList();

        if (groups.Count is 0 or > 12)
        {
            return null;
        }

        return new
        {
            key,
            distinct = groups.Count,
            counts = groups.Take(8).Select(group => new { value = group.Key, count = group.Count() })
        };
    }

    private static List<Dictionary<string, object?>> Highlights(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> keys,
        string sortKey)
    {
        var ranked = new List<(IReadOnlyDictionary<string, object?> Row, decimal Value)>();
        foreach (var row in rows)
        {
            if (TryNumber(Read(row, sortKey), out var value))
            {
                ranked.Add((row, value));
            }
        }

        ranked.Sort((left, right) => left.Value.CompareTo(right.Value));
        var picked = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var item in ranked.Take(HighlightCount))
        {
            picked.Add(item.Row);
        }

        foreach (var item in ranked.TakeLast(HighlightCount))
        {
            if (!picked.Contains(item.Row))
            {
                picked.Add(item.Row);
            }
        }

        return picked.Select(row =>
        {
            var projected = Project(row, keys);
            projected["highlight"] = "extreme";
            return projected;
        }).ToList();
    }

    private static string? PickLabelKey(
        IReadOnlyList<string> keys,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        string[] preferred = ["name", "clientName", "title", "ingredient", "supplier", "driverName"];
        foreach (var name in preferred)
        {
            var match = keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !IsMostlyNumeric(rows, match))
            {
                return match;
            }
        }

        return keys.FirstOrDefault(key => !IsMostlyNumeric(rows, key));
    }

    private static bool IsMostlyNumeric(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        string key)
    {
        var seen = 0;
        var numeric = 0;
        foreach (var row in rows)
        {
            var value = Read(row, key);
            if (value is null)
            {
                continue;
            }

            seen++;
            if (TryNumber(value, out _))
            {
                numeric++;
            }
        }

        return seen > 0 && numeric * 2 >= seen;
    }

    private static object? Read(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) ? value : null;

    private static bool IsIdKey(string key) =>
        key.Equals("id", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith("Id", StringComparison.Ordinal);

    private static bool TryNumber(object? value, out decimal number)
    {
        switch (value)
        {
            case decimal dec:
                number = dec;
                return true;
            case double dbl:
                number = (decimal)dbl;
                return true;
            case float flt:
                number = (decimal)flt;
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case string text when decimal.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out number):
                return true;
            default:
                number = 0;
                return false;
        }
    }

    public static DateOnly? ReadDateOnly(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        var raw = node.GetValue<string?>();
        return DateOnly.TryParse(raw, out var date) ? date : null;
    }

    public static Guid? ReadGuid(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        var raw = node.GetValue<string?>();
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static string? ReadString(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        return node.GetValue<string?>();
    }

    public static bool? ReadBool(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        return node.GetValue<bool?>();
    }

    public static int ReadInt(JsonObject args, string name, int fallback)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node is null)
        {
            return fallback;
        }

        return node.GetValue<int?>() ?? fallback;
    }

    public static TimeOnly? ReadTimeOnly(JsonObject args, string name)
    {
        var raw = ReadString(args, name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return TimeOnly.TryParse(raw.Trim(), out var time) ? time : null;
    }
}
