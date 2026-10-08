using System.Text;
using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Configuration;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class SearchKnowledgeBaseTool : IAssistantTool
{
    private readonly IKnowledgeSearchQueries _knowledge;
    private readonly AssistantRagOptions _rag;

    public SearchKnowledgeBaseTool(
        IKnowledgeSearchQueries knowledge,
        IOptions<AssistantRagOptions> rag)
    {
        _knowledge = knowledge;
        _rag = rag.Value;
    }

    public string Name => "search_knowledge_base";

    public string Description =>
        "Search through uploaded company documents, recipes, standards, HACCP rules, delivery contracts, and kitchen manuals. " +
        "Returns document chunk text that you MUST use as the sole factual source for the answer.";

    public JsonObject ParametersSchema => new()
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
                ["description"] = $"Number of chunks to return (1-{Math.Clamp(_rag.MaxTopK, 1, 10)}). Default {_rag.DefaultTopK}."
            }
        },
        ["required"] = new JsonArray("query")
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        var maxTopK = Math.Clamp(_rag.MaxTopK, 1, 10);
        var defaultTopK = Math.Clamp(_rag.DefaultTopK, 1, maxTopK);
        var maxCharsPerChunk = Math.Max(200, _rag.MaxCharsPerChunk);
        var maxTotalChars = Math.Max(maxCharsPerChunk, _rag.MaxTotalContextChars);

        var query = (ReportArtifactMapper.ReadString(args, "query") ?? string.Empty).Trim();
        var topK = Math.Clamp(ReportArtifactMapper.ReadInt(args, "topK", defaultTopK), 1, maxTopK);

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

        var chunks = new List<(int Rank, string Title, string FileName, int ChunkIndex, double Distance, string Content, string? DownloadUrl)>(hits.Count);
        var budget = maxTotalChars;
        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            if (budget <= 100)
            {
                break;
            }

            var allowed = Math.Min(maxCharsPerChunk, budget);
            var content = FitChunk(hit.Content ?? string.Empty, allowed);
            budget -= content.Length;
            chunks.Add((
                i + 1,
                hit.DocumentTitle,
                hit.FileName,
                hit.ChunkIndex,
                Math.Round(hit.Distance, 4),
                content,
                hit.DownloadUrl));
        }

        var documentChunks = chunks.Count == 0
            ? "No relevant documents were found. Do not invent policy or recipe text."
            : BuildDocumentChunksBlock(chunks);

        var sources = chunks
            .GroupBy(c => c.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var excerpt = string.Join("\n\n", group.Select(c => c.Content));
                return new KnowledgeSourceItem(
                    first.Title,
                    first.FileName,
                    FitChunk(excerpt, 1200),
                    first.DownloadUrl);
            })
            .ToList();

        var artifacts = new List<AssistantArtifact>
        {
            new KnowledgeSourcesArtifact("Knowledge sources", sources)
        };

        return new ToolResult(
            new
            {
                answerFrom =
                    "Rely ONLY on [DOCUMENT CHUNKS] / <context>. Translate into the user's language. Cite document titles. " +
                    "Write <<<CHAT>>> (short answer + source title) and <<<OVERVIEW>>> (detailed explanation from the chunks). " +
                    "Do not invent facts missing from the context.",
                count = chunks.Count,
                relevant = chunks.Count > 0,
                documentChunks
            },
            artifacts);
    }

    private static string BuildDocumentChunksBlock(
        IReadOnlyList<(int Rank, string Title, string FileName, int ChunkIndex, double Distance, string Content, string? DownloadUrl)> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[DOCUMENT CHUNKS]");
        sb.AppendLine("<context>");
        for (var i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i];
            if (i > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine($"--- {c.Title} ({c.FileName}) ---");
            sb.AppendLine(c.Content);
        }

        sb.AppendLine("</context>");
        sb.Append("[/DOCUMENT CHUNKS]");
        return sb.ToString();
    }

    private static string FitChunk(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value;
        }

        if (max <= 1)
        {
            return "…";
        }

        var slice = value[..max];
        var breakAt = Math.Max(
            slice.LastIndexOf('\n'),
            Math.Max(slice.LastIndexOf(". ", StringComparison.Ordinal), slice.LastIndexOf(' ')));
        if (breakAt >= max / 2)
        {
            slice = slice[..breakAt].TrimEnd();
        }
        else
        {
            slice = slice.TrimEnd();
        }

        return slice + "…";
    }
}
