using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetOperationsPulseTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetOperationsPulseTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_operations_pulse";

    public string Description =>
        "Operations pulse for one day or any calendar period: orders, revenue, portions, kitchen readiness, " +
        "and the current critical-stock snapshot. One call covers a week, month, year, or explicit range. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = AssistantPeriod.Schema();

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        return AssistantPeriod.ForCalendar(
            args,
            _clock,
            period => _reporting.GetOperationsPulseAsync(period.DateFrom, period.DateTo, ct));
    }
}
