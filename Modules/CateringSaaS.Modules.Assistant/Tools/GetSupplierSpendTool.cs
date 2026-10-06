using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetSupplierSpendTool : IAssistantTool
{
    private readonly IReportingService _reporting;

    public GetSupplierSpendTool(IReportingService reporting)
    {
        _reporting = reporting;
    }

    public string Name => "get_supplier_spend";

    public string Description =>
        "Supplier spend and purchase volume analysis (cost, quantity, receipt count). " +
        "Optional supplierId filter. dateFrom/dateTo are local ISO dates. " +
        "Optional timeFrom/timeTo (HH:mm) limit receipts to a local clock window.";

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
            ["supplierId"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional supplier GUID."
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
        var supplierId = ReportArtifactMapper.ReadGuid(args, "supplierId");
        var timeFrom = ReportArtifactMapper.ReadTimeOnly(args, "timeFrom");
        var timeTo = ReportArtifactMapper.ReadTimeOnly(args, "timeTo");
        var report = await _reporting.GetSupplierSpendAsync(
            dateFrom, dateTo, supplierId, ct, timeFrom, timeTo);
        return ReportArtifactMapper.FromReport(report);
    }
}
