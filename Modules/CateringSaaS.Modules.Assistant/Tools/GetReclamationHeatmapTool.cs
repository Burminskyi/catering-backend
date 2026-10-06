using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetReclamationHeatmapTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetReclamationHeatmapTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_reclamation_heatmap";

    public string Description =>
        "Reclamation heat map of low meal ratings by dish and client. " +
        "Optional clientCompanyId and maxRating (1-5, default typically 3). " +
        "dateFrom/dateTo are ISO yyyy-MM-dd.";

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
            ["clientCompanyId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional client company GUID from resolve_client."
            },
            ["maxRating"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Include reviews with rating <= this value (1-5). Default service threshold applies if omitted."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var dateFrom = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var dateTo = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        var clientId = ReportArtifactMapper.ReadGuid(args, "clientCompanyId");
        var maxRating = args.TryGetPropertyValue("maxRating", out var node) && node is not null
            ? node.GetValue<int?>()
            : null;
        var report = await _reporting.GetReclamationHeatMapAsync(dateFrom, dateTo, clientId, maxRating, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
