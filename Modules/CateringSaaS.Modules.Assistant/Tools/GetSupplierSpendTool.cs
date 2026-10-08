using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetSupplierSpendTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetSupplierSpendTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_supplier_spend";

    public string Description =>
        "Supplier spend and purchase volume. Optional supplierId. " +
        "lastHours filters receipts. timeFrom/timeTo is a daily shift, not a rolling window. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = WithShift(AssistantPeriod.Schema(
        ("supplierId", new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional supplier GUID."
        })));

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var supplierId = ReportArtifactMapper.ReadGuid(args, "supplierId");
        return AssistantPeriod.ForMovements(
            args,
            _clock,
            (period, shiftFrom, shiftTo) => _reporting.GetSupplierSpendAsync(
                period.DateFrom,
                period.DateTo,
                supplierId,
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
