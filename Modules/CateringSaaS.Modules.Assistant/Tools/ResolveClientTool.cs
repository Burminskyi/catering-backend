using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class ResolveClientTool : IAssistantTool
{
    private readonly IClientCompanyLookup _clients;

    public ResolveClientTool(IClientCompanyLookup clients)
    {
        _clients = clients;
    }

    public string Name => "resolve_client";

    public string Description =>
        "Search B2B client companies in the current workspace by partial name. " +
        "Returns matching client IDs and contact details. Use before filtering reports by client.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Partial client company name (e.g. EPAM, SoftServe)."
            },
            ["limit"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Max matches to return (1-20). Default 10."
            }
        },
        ["required"] = new JsonArray("query")
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        var query = ReportArtifactMapper.ReadString(args, "query") ?? string.Empty;
        var limit = ReportArtifactMapper.ReadInt(args, "limit", 10);

        var matches = await _clients.SearchByNameAsync(scope.WorkspaceId, query, limit, ct);

        var rows = matches.Select(m => new Dictionary<string, object?>
        {
            ["clientCompanyId"] = m.Id,
            ["name"] = m.Name,
            ["address"] = m.Address,
            ["contactName"] = m.ContactName,
            ["contactPhone"] = m.ContactPhone
        }).ToList();

        var artifacts = new List<AssistantArtifact>
        {
            new TableArtifact(
                "Matching clients",
                [
                    new ArtifactColumn("name", "Company"),
                    new ArtifactColumn("clientCompanyId", "Client ID"),
                    new ArtifactColumn("contactName", "Contact"),
                    new ArtifactColumn("contactPhone", "Phone")
                ],
                rows)
        };

        return new ToolResult(
            new { count = matches.Count, clients = matches },
            artifacts);
    }
}
