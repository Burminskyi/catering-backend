using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetShortageForecastTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetShortageForecastTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_shortage_forecast";

    public string Description =>
        "Kitchen shopping and ingredient shortage forecast for a target production/delivery date. " +
        "Optional onlyDeficits=true to return only ingredients that will fall short.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["targetDate"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Target ISO date (yyyy-MM-dd). Defaults to current UTC day."
            },
            ["onlyDeficits"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "When true, return only shortage rows."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var targetDate = ReportArtifactMapper.ReadDateOnly(args, "targetDate");
        var onlyDeficits = ReportArtifactMapper.ReadBool(args, "onlyDeficits");
        var report = await _reporting.GetShortageForecastAsync(targetDate, onlyDeficits, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
