using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetRevenueByClientTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetRevenueByClientTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_revenue_by_client";

    public string Description =>
        "Revenue and order volume grouped by B2B client for a date range. " +
        "Optional clientCompanyId filter (resolve via resolve_client first). " +
        "dateFrom/dateTo are ISO yyyy-MM-dd; omit to use the service default range.";

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
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var dateFrom = ReportArtifactMapper.ReadDateOnly(args, "dateFrom");
        var dateTo = ReportArtifactMapper.ReadDateOnly(args, "dateTo");
        var clientId = ReportArtifactMapper.ReadGuid(args, "clientCompanyId");
        var report = await _reporting.GetRevenueByClientAsync(dateFrom, dateTo, clientId, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
