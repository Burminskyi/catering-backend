using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetOperationsPulseTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetOperationsPulseTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_operations_pulse";

    public string Description =>
        "Today's operations pulse: orders, revenue, portions, kitchen readiness, critical stock, " +
        "assigned/unassigned deliveries. Optional targetDate (ISO yyyy-MM-dd), defaults to today UTC.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["targetDate"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional ISO date (yyyy-MM-dd). Defaults to current UTC day."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var targetDate = ReportArtifactMapper.ReadDateOnly(args, "targetDate");
        var report = await _reporting.GetTodayPulseAsync(targetDate, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
