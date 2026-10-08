using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetDriverEfficiencyTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetDriverEfficiencyTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_driver_efficiency";

    public string Description =>
        "Driver fulfillment: delivered orders, portions, and revenue per driver. Optional driverId. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = AssistantPeriod.Schema(
        ("driverId", new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional driver user GUID."
        }));

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var driverId = ReportArtifactMapper.ReadGuid(args, "driverId");
        return AssistantPeriod.ForCalendar(
            args,
            _clock,
            period => _reporting.GetDriverEfficiencyAsync(period.DateFrom, period.DateTo, driverId, ct));
    }
}
