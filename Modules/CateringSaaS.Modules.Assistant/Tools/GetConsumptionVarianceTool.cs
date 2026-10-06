using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetConsumptionVarianceTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetConsumptionVarianceTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_consumption_variance";

    public string Description =>
        "Ingredient consumption vs expected dish-production usage (variance / over-consume). " +
        "Optional ingredientId filter. dateFrom/dateTo are ISO yyyy-MM-dd.";

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
            ["ingredientId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional ingredient GUID to focus on one SKU."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var dateFrom = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var dateTo = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        var ingredientId = ReportArtifactMapper.ReadGuid(args, "ingredientId");
        var report = await _reporting.GetConsumptionVarianceAsync(dateFrom, dateTo, ingredientId, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
