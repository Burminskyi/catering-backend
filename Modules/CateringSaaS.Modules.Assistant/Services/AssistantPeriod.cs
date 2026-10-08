using System.Globalization;
using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Services;

/// <summary>
/// Resolves tool period arguments against the client timezone.
/// The model names the intent (preset, lastDays, lastHours, or explicit dates).
/// Calendar math stays here.
/// </summary>
internal sealed record AssistantPeriod(
    DateOnly DateFrom,
    DateOnly DateTo,
    bool IsInstant,
    DateTime? InstantFromUtc,
    DateTime? InstantToUtcExclusive,
    string Label)
{
    public const string ParameterHint =
        "Pass preset, lastDays, lastHours, or date/dateFrom/dateTo. Do not calculate the dates yourself. " +
        "Weeks start Monday. this week/month/year ends today. Default is today.";

    public bool IsSingleDay => !IsInstant && DateFrom == DateTo;

    public static JsonObject Schema(params (string Name, JsonObject Node)[] extra)
    {
        var properties = new JsonObject();
        AddParameters(properties);
        foreach (var (name, node) in extra)
        {
            properties[name] = node;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };
    }

    public static async Task<ToolResult> ForCalendar(
        JsonObject args,
        IClientTimeContext clock,
        Func<AssistantPeriod, Task<CateringSaaS.Shared.Reporting.ReportResponse>> load)
    {
        var period = Resolve(args, clock);
        if (period.IsInstant)
        {
            return BusinessDateOnly(period);
        }

        return ReportArtifactMapper.FromReport(await load(period), period.Label);
    }

    public static async Task<ToolResult> ForMovements(
        JsonObject args,
        IClientTimeContext clock,
        Func<AssistantPeriod, TimeOnly?, TimeOnly?, Task<CateringSaaS.Shared.Reporting.ReportResponse>> load)
    {
        var period = Resolve(args, clock);
        var shiftFrom = period.IsInstant ? null : ReportArtifactMapper.ReadTimeOnly(args, "timeFrom");
        var shiftTo = period.IsInstant ? null : ReportArtifactMapper.ReadTimeOnly(args, "timeTo");
        var report = await load(period, shiftFrom, shiftTo);
        var label = period.IsInstant
            ? period.Label + ". This clock window filters stock movements. Order figures in the same report use the calendar days covered."
            : period.Label;
        return ReportArtifactMapper.FromReport(report, label);
    }

    public static void AddParameters(JsonObject properties)
    {
        properties["preset"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] =
                "Relative calendar period. One of: today, yesterday, this_week, last_week, next_week, " +
                "this_month, last_month, next_month, this_year, last_year, next_year. " +
                "Do not calculate the dates yourself."
        };
        properties["lastDays"] = new JsonObject
        {
            ["type"] = "integer",
            ["description"] = "Last N local calendar days including today. Example: last 3 days → 3."
        };
        properties["lastHours"] = new JsonObject
        {
            ["type"] = "integer",
            ["description"] =
                "Rolling window ending now, in hours. Example: last 3 hours → 3. " +
                "Only stock-movement tools can apply this. Order tools will say the dataset has no time of day."
        };
        properties["date"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = "One local calendar day yyyy-MM-dd. Alias: targetDate."
        };
        properties["dateFrom"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Inclusive range start yyyy-MM-dd, client local. Use with dateTo."
        };
        properties["dateTo"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Inclusive range end yyyy-MM-dd, client local."
        };
    }

    public static AssistantPeriod Resolve(JsonObject args, IClientTimeContext clock)
    {
        var today = clock.LocalToday;
        var lastHours = ReportArtifactMapper.ReadInt(args, "lastHours", 0);
        if (lastHours > 0)
        {
            var hours = Math.Clamp(lastHours, 1, 24 * 14);
            var end = clock.LocalNow;
            var start = end.AddHours(-hours);
            return new AssistantPeriod(
                DateOnly.FromDateTime(start.DateTime),
                DateOnly.FromDateTime(end.DateTime),
                true,
                start.UtcDateTime,
                end.UtcDateTime,
                $"{start:yyyy-MM-dd HH:mm} – {end:yyyy-MM-dd HH:mm} {clock.TimeZoneId}");
        }

        var preset = NormalizePreset(ReportArtifactMapper.ReadString(args, "preset"));
        if (preset is not null && TryPreset(preset, today, out var presetFrom, out var presetTo))
        {
            return Calendar(presetFrom, presetTo);
        }

        var lastDays = ReportArtifactMapper.ReadInt(args, "lastDays", 0);
        if (lastDays > 0)
        {
            var days = Math.Clamp(lastDays, 1, 366);
            return Calendar(today.AddDays(1 - days), today);
        }

        var date = ReportArtifactMapper.ReadDateOnly(args, "date")
            ?? ReportArtifactMapper.ReadDateOnly(args, "targetDate");
        var from = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var to = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        if (from is null && to is null && date is not null)
        {
            return Calendar(date.Value, date.Value);
        }

        if (from is not null || to is not null)
        {
            var end = to ?? date ?? from!.Value;
            var start = from ?? date ?? end;
            if (start > end)
            {
                (start, end) = (end, start);
            }

            return Calendar(start, end);
        }

        return Calendar(today, today);
    }

    public static ToolResult BusinessDateOnly(AssistantPeriod period) =>
        new(new
        {
            answerFrom = "Tell the user this dataset is stored by business date only, so an hourly window cannot be applied. Ask for a day or a date range.",
            period = period.Label,
            hourlySupported = false
        });

    private static AssistantPeriod Calendar(DateOnly from, DateOnly to) =>
        new(
            from,
            to,
            false,
            null,
            null,
            from == to ? from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : $"{from:yyyy-MM-dd} – {to:yyyy-MM-dd}");

    private static string? NormalizePreset(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        return text switch
        {
            "thisweek" => "this_week",
            "lastweek" => "last_week",
            "nextweek" => "next_week",
            "thismonth" => "this_month",
            "lastmonth" => "last_month",
            "nextmonth" => "next_month",
            "thisyear" => "this_year",
            "lastyear" => "last_year",
            "nextyear" => "next_year",
            _ => text
        };
    }

    private static bool TryPreset(string preset, DateOnly today, out DateOnly from, out DateOnly to)
    {
        var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        switch (preset)
        {
            case "today":
                from = to = today;
                return true;
            case "yesterday":
                from = to = today.AddDays(-1);
                return true;
            case "this_week":
                from = monday;
                to = today;
                return true;
            case "last_week":
                from = monday.AddDays(-7);
                to = monday.AddDays(-1);
                return true;
            case "next_week":
                from = monday.AddDays(7);
                to = monday.AddDays(13);
                return true;
            case "this_month":
                from = new DateOnly(today.Year, today.Month, 1);
                to = today;
                return true;
            case "last_month":
                var prev = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
                from = prev;
                to = prev.AddMonths(1).AddDays(-1);
                return true;
            case "next_month":
                var next = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
                from = next;
                to = next.AddMonths(1).AddDays(-1);
                return true;
            case "this_year":
                from = new DateOnly(today.Year, 1, 1);
                to = today;
                return true;
            case "last_year":
                from = new DateOnly(today.Year - 1, 1, 1);
                to = new DateOnly(today.Year - 1, 12, 31);
                return true;
            case "next_year":
                from = new DateOnly(today.Year + 1, 1, 1);
                to = new DateOnly(today.Year + 1, 12, 31);
                return true;
            default:
                from = to = today;
                return false;
        }
    }
}
