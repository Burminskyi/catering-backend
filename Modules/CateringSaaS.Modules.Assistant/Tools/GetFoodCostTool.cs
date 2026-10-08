using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetFoodCostTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetFoodCostTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_food_cost";

    public string Description =>
        "Food-cost percentage: purchases, consumption, and order revenue. " +
        "lastHours filters stock movements. timeFrom/timeTo is a daily shift window, not a rolling hour range. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = WithShift(AssistantPeriod.Schema());

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        return AssistantPeriod.ForMovements(
            args,
            _clock,
            (period, shiftFrom, shiftTo) => _reporting.GetFoodCostAsync(
                period.DateFrom,
                period.DateTo,
                ct,
                shiftFrom,
                shiftTo,
                period.InstantFromUtc,
                period.InstantToUtcExclusive));
    }

    private static JsonObject WithShift(JsonObject schema)
    {
        var properties = schema["properties"]!.AsObject();
        properties["timeFrom"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional daily shift start HH:mm. Not used for lastHours."
        };
        properties["timeTo"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional daily shift end HH:mm. Not used for lastHours."
        };
        return schema;
    }
}
