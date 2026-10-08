using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Modules.Reporting.Services;
using CateringSaaS.Shared.MultiTenancy;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class GetReclamationHeatmapTool : IAssistantTool
{
    private readonly IReportingService _reporting;
    private readonly IClientTimeContext _clock;

    public GetReclamationHeatmapTool(IReportingService reporting, IClientTimeContext clock)
    {
        _reporting = reporting;
        _clock = clock;
    }

    public string Name => "get_reclamation_heatmap";

    public string Description =>
        "Reclamation heat map of low meal ratings by dish and client. Optional clientCompanyId and maxRating. " +
        AssistantPeriod.ParameterHint;

    public JsonObject ParametersSchema { get; } = AssistantPeriod.Schema(
        ("clientCompanyId", new JsonObject
        {
            ["type"] = "string",
            ["description"] = "Optional client company GUID from resolve_client."
        }),
        ("maxRating", new JsonObject
        {
            ["type"] = "integer",
            ["description"] = "Include reviews with rating <= this value (1-5)."
        }));

    public Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        _ = scope;
        var clientId = ReportArtifactMapper.ReadGuid(args, "clientCompanyId");
        int? maxRating = args.TryGetPropertyValue("maxRating", out var node) && node is not null
            ? node.GetValue<int?>()
            : null;
        return AssistantPeriod.ForCalendar(
            args,
            _clock,
            period => _reporting.GetReclamationHeatMapAsync(period.DateFrom, period.DateTo, clientId, maxRating, ct));
    }
}
