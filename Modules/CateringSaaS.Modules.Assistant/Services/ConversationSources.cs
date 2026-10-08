using System.Text.Json;
using CateringSaaS.Modules.Assistant.Contracts;
using OpenAI.Chat;

namespace CateringSaaS.Modules.Assistant.Services;

/// <summary>
/// Accumulated data origins for a conversation: knowledge base, operational DB tools, or chat-only.
/// </summary>
internal static class ConversationSources
{
    public const string Knowledge = "knowledge";
    public const string Operations = "operations";
    public const string Chat = "chat";

    private static readonly string[] Order = [Knowledge, Operations, Chat];

    public static IReadOnlyList<string> Detect(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<AssistantArtifact>? artifacts)
    {
        var hasKnowledge = false;
        var hasOperations = false;

        if (artifacts is { Count: > 0 })
        {
            foreach (var artifact in artifacts)
            {
                switch (artifact)
                {
                    case KnowledgeSourcesArtifact:
                        hasKnowledge = true;
                        break;
                    case TableArtifact or MetricArtifact:
                        hasOperations = true;
                        break;
                }
            }
        }

        foreach (var message in messages)
        {
            if (message is not ToolChatMessage tool)
            {
                continue;
            }

            var text = string.Concat(tool.Content.Select(part => part.Text));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (text.Contains("[DOCUMENT CHUNKS]", StringComparison.Ordinal)
                || text.Contains("\"documentChunks\"", StringComparison.Ordinal)
                || text.Contains("knowledgeSources", StringComparison.OrdinalIgnoreCase))
            {
                hasKnowledge = true;
            }
            else
            {
                // Any other tool payload is treated as operational/DB data.
                hasOperations = true;
            }
        }

        if (!hasKnowledge && !hasOperations)
        {
            return [Chat];
        }

        var result = new List<string>(2);
        if (hasKnowledge)
        {
            result.Add(Knowledge);
        }

        if (hasOperations)
        {
            result.Add(Operations);
        }

        return result;
    }

    public static string MergeJson(string? existingJson, IReadOnlyList<string> incoming)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Parse(existingJson))
        {
            set.Add(item);
        }

        foreach (var item in incoming)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                set.Add(item.Trim().ToLowerInvariant());
            }
        }

        // Drop "chat" once a real data source appears.
        if (set.Contains(Knowledge) || set.Contains(Operations))
        {
            set.Remove(Chat);
        }

        if (set.Count == 0)
        {
            set.Add(Chat);
        }

        var ordered = Order.Where(set.Contains).ToList();
        return JsonSerializer.Serialize(ordered);
    }

    public static IReadOnlyList<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return list
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => s is Knowledge or Operations or Chat)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => Array.IndexOf(Order, s))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<string> InferFromArtifactsJson(string? artifactsJson)
    {
        if (string.IsNullOrWhiteSpace(artifactsJson))
        {
            return [Chat];
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var artifacts = JsonSerializer.Deserialize<List<AssistantArtifact>>(artifactsJson, options);
            return Detect([], artifacts);
        }
        catch (JsonException)
        {
            return [Chat];
        }
    }
}
