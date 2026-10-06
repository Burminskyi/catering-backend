using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetDriverEfficiencyTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetDriverEfficiencyTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_driver_efficiency";

    public string Description =>
        "Driver fulfillment and efficiency: delivered orders, portions, and revenue per driver. " +
        "Optional driverId filter. dateFrom/dateTo are ISO yyyy-MM-dd.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["dateFrom"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Range start ISO date (yyyy-MM-dd)."
            },
            ["dateTo"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Range end ISO date (yyyy-MM-dd)."
            },
            ["driverId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional driver user GUID."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var dateFrom = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var dateTo = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        var driverId = ReportArtifactMapper.ReadGuid(args, "driverId");
        var report = await _reporting.GetDriverEfficiencyAsync(dateFrom, dateTo, driverId, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
