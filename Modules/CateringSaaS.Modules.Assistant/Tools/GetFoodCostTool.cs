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
        "plus adjustments/spoilage. dateFrom/dateTo are ISO yyyy-MM-dd.";

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
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var dateFrom = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var dateTo = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        var report = await _reporting.GetFoodCostAsync(dateFrom, dateTo, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
