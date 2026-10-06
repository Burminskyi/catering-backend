using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetCancellationsTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetCancellationsTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_cancellations";

    public string Description =>
        "Cancelled order volume and lost revenue by client. " +
        "Optional clientCompanyId filter. dateFrom/dateTo are ISO yyyy-MM-dd.";

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
        var report = await _reporting.GetCancellationsAsync(dateFrom, dateTo, clientId, ct);
        return ReportArtifactMapper.FromReport(report);
    }
}
