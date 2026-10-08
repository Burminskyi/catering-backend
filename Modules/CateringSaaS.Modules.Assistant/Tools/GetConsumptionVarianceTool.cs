using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetConsumptionVarianceTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetConsumptionVarianceTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_consumption_variance";

    public string Description =>
        "Ingredient consumption versus expected usage from dishes. Optional ingredientId. " +
        "lastHours filters actual stock consumption. timeFrom/timeTo is a daily shift, not a rolling window. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = WithShift(AssistantPeriod.Schema(
        ("ingredientId", new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional ingredient GUID."
        })));

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var ingredientId = ReportArtifactMapper.ReadGuid(args, "ingredientId");
        return AssistantPeriod.ForMovements(
            args,
            _clock,
            (period, shiftFrom, shiftTo) => _reporting.GetConsumptionVarianceAsync(
                period.DateFrom,
                period.DateTo,
                ingredientId,
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
