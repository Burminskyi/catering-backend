using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class SearchKnowledgeBaseTool : IAssistantTool
{
    private readonly IKnowledgeSearchQueries _knowledge;

    public SearchKnowledgeBaseTool(IKnowledgeSearchQueries knowledge)
    {
        _knowledge = knowledge;
    }

    public string Name => "search_knowledge_base";

    public string Description =>
        "Search through uploaded company documents, recipes, standards, HACCP rules, delivery contracts, and kitchen manuals.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Natural-language search query against the workspace knowledge base."
            },
            ["topK"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Number of chunks to return (1-10). Default 3."
            }
        },
        ["required"] = new JsonArray("query")
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        var query = (ReportArtifactMapper.ReadString(args, "query") ?? string.Empty).Trim();
        var topK = Math.Clamp(ReportArtifactMapper.ReadInt(args, "topK", 3), 1, 10);

        if (query.Length == 0)
        {
            return new ToolResult(new
            {
                count = 0,
                message = "A non-empty query is required.",
                items = Array.Empty<object>()
            });
        }

        var hits = await _knowledge.SearchAsync(scope.WorkspaceId, query, topK, ct);

        var items = hits.Select((hit, index) => new Dictionary<string, object?>
        {
            ["rank"] = index + 1,
            ["documentTitle"] = hit.DocumentTitle,
            ["fileName"] = hit.FileName,
            ["chunkIndex"] = hit.ChunkIndex,
            ["distance"] = Math.Round(hit.Distance, 4),
            ["content"] = hit.Content
        }).ToList();

        var formattedForLlm = hits.Count == 0
            ? "No relevant documents were found. The closest chunks were below the similarity threshold, so do not answer from the knowledge base."
            : string.Join(
                "\n\n---\n\n",
                hits.Select((hit, index) =>
                    $"[{index + 1}] {hit.DocumentTitle} ({hit.FileName}), chunk #{hit.ChunkIndex}\n{hit.Content}"));

        var artifacts = new List<AssistantArtifact>
        {
            new TableArtifact(
                "Knowledge matches",
                [
                    new ArtifactColumn("rank", "#"),
                    new ArtifactColumn("documentTitle", "Document"),
                    new ArtifactColumn("chunkIndex", "Chunk"),
                    new ArtifactColumn("distance", "Distance"),
                    new ArtifactColumn("content", "Excerpt")
                ],
                items.Select(row => new Dictionary<string, object?>
                {
                    ["rank"] = row["rank"],
                    ["documentTitle"] = row["documentTitle"],
                    ["chunkIndex"] = row["chunkIndex"],
                    ["distance"] = row["distance"],
                    ["content"] = Truncate(row["content"]?.ToString() ?? string.Empty, 240)
                }).ToList()),
            new MetricArtifact("Matched chunks", hits.Count.ToString())
        };

        return new ToolResult(
            new
            {
            count = hits.Count,
            relevant = hits.Count > 0,
            query,
                topK,
                formattedContext = formattedForLlm,
                items
            },
            artifacts);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
