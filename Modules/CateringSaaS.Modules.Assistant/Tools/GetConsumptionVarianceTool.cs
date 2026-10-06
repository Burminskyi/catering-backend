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
        "Optional ingredientId filter. dateFrom/dateTo are local ISO dates. " +
        "Optional timeFrom/timeTo (HH:mm) limit actual stock consumption to a local clock window.";

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
        var ingredientId = ReportArtifactMapper.ReadGuid(args, "ingredientId");
        var timeFrom = ReportArtifactMapper.ReadTimeOnly(args, "timeFrom");
        var timeTo = ReportArtifactMapper.ReadTimeOnly(args, "timeTo");
        var report = await _reporting.GetConsumptionVarianceAsync(
            dateFrom, dateTo, ingredientId, ct, timeFrom, timeTo);
        return ReportArtifactMapper.FromReport(report);
    }
}
