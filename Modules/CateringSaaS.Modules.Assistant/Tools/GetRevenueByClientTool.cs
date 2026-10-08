using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetRevenueByClientTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetRevenueByClientTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_revenue_by_client";

    public string Description =>
        "Revenue and order volume grouped by B2B client. Optional clientCompanyId. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = AssistantPeriod.Schema(
        ("clientCompanyId", new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional client company GUID from resolve_client."
        }));

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var clientId = ReportArtifactMapper.ReadGuid(args, "clientCompanyId");
        return AssistantPeriod.ForCalendar(
            args,
            _clock,
            period => _reporting.GetRevenueByClientAsync(period.DateFrom, period.DateTo, clientId, ct));
    }
}
