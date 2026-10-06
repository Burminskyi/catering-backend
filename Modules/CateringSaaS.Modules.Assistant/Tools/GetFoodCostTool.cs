using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetFoodCostTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetFoodCostTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_food_cost";

    public string Description =>
        "Food-cost percentage and usage efficiency: purchase vs consume vs order revenue, " +
        "plus adjustments/spoilage. dateFrom/dateTo are local ISO dates (yyyy-MM-dd). " +
        "Optional timeFrom/timeTo are local HH:mm bounds on stock movements.";

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
            ["timeFrom"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional local start time HH:mm (24-hour)."
            },
            ["timeTo"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional local end time HH:mm (24-hour), exclusive."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var dateFrom = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var dateTo = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        var timeFrom = ReportArtifactMapper.ReadTimeOnly(args, "timeFrom");
        var timeTo = ReportArtifactMapper.ReadTimeOnly(args, "timeTo");
        var report = await _reporting.GetFoodCostAsync(dateFrom, dateTo, ct, timeFrom, timeTo);
        return ReportArtifactMapper.FromReport(report);
    }
}
