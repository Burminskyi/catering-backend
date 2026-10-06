using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetDeliveryAndReclamationsTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetDeliveryAndReclamationsTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_delivery_and_reclamations";

    public string Description =>
        "Client delivery audit with reviews and reclamations (low ratings). " +
        "Optional dateFrom/dateTo (ISO yyyy-MM-dd) and clientCompanyId filter.";

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
        var report = await _reporting.GetDeliveryAuditAsync(dateFrom, dateTo, clientId, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
