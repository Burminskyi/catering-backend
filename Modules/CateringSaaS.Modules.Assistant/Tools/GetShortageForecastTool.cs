using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetShortageForecastTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetShortageForecastTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_shortage_forecast";

    public string Description =>
        "Ingredient shortage forecast for confirmed orders on one day or a calendar period. " +
        "Optional onlyDeficits. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = AssistantPeriod.Schema(
        ("onlyDeficits", new JsonObject
        {
            ["type"] = "boolean",
            ["description"] = "When true, return only shortage rows."
        }));

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var onlyDeficits = ReportArtifactMapper.ReadBool(args, "onlyDeficits");
        return AssistantPeriod.ForCalendar(
            args,
            _clock,
            period => _reporting.GetShortageForecastAsync(
                period.DateFrom,
                onlyDeficits,
                ct,
                period.DateFrom,
                period.DateTo));
    }
}
