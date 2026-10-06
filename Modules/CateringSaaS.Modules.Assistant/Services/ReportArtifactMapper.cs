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

    public static ToolResult FromReport(ReportResponse report)
    {
        var artifacts = new List<AssistantArtifact>();

        foreach (var metric in report.Metrics.Take(8))
        {
            artifacts.Add(new MetricArtifact(metric.Label, metric.DisplayValue, report.Title));
        }

        foreach (var table in report.Tables.Take(3))
        {
            artifacts.Add(new TableArtifact(
                table.Title,
                table.Columns.Select(c => new ArtifactColumn(c.Key, c.Label)).ToList(),
                table.Rows.Select(r => r.ToDictionary(kv => kv.Key, kv => kv.Value)).ToList()));
        }

        return new ToolResult(report, artifacts);
    }

    public static string ToToolJson(ToolResult result) =>
        JsonSerializer.Serialize(result.Data, JsonOptions);

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
