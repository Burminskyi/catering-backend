using System.Text.RegularExpressions;

namespace CateringSaaS.Modules.Assistant.Services;

internal static partial class AssistantReplySplitter
{
    private static readonly Regex TitleMarker = TitleMarkerRegex();
    private static readonly Regex ChatMarker = ChatMarkerRegex();
    private static readonly Regex OverviewMarker = OverviewMarkerRegex();
    private static readonly Regex MarkdownTableBlock = MarkdownTableBlockRegex();

    /// <summary>
    /// Splits a model reply into an optional title, short chat bubble, and Overview panel body.
    /// </summary>
    public static (string Chat, string? Overview, string? Title) Split(string? raw, bool preferOverviewFallback)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return (string.Empty, null, null);
        }

        var markers = new List<(int Index, int Length, string Kind)>();
        Collect(markers, TitleMarker.Matches(text), "title");
        Collect(markers, ChatMarker.Matches(text), "chat");
        Collect(markers, OverviewMarker.Matches(text), "overview");
        markers.Sort((a, b) => a.Index.CompareTo(b.Index));

        if (markers.Count > 0)
        {
            string? title = null;
            string? chat = null;
            string? overview = null;
            for (var i = 0; i < markers.Count; i++)
            {
                var start = markers[i].Index + markers[i].Length;
                var end = i + 1 < markers.Count ? markers[i + 1].Index : text.Length;
                var body = text[start..end].Trim();
                switch (markers[i].Kind)
                {
                    case "title":
                        title = body;
                        break;
                    case "chat":
                        chat = body;
                        break;
                    case "overview":
                        overview = body;
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(chat) && !string.IsNullOrWhiteSpace(overview))
            {
                return (chat, StripSummaryHeading(overview), ConversationTitle.Normalize(title));
            }

            if (!string.IsNullOrWhiteSpace(chat))
            {
                return (chat, null, ConversationTitle.Normalize(title));
            }
        }

        if (preferOverviewFallback)
        {
            var chatOnly = StripMarkdownTables(text).Trim();
            if (string.IsNullOrWhiteSpace(chatOnly))
            {
                chatOnly = text;
            }

            if (chatOnly.Length > 700)
            {
                chatOnly = TruncateAtSentence(chatOnly, 520);
            }

            return (chatOnly, StripSummaryHeading(text), null);
        }

        return (text, null, null);
    }

    private static void Collect(List<(int Index, int Length, string Kind)> markers, MatchCollection matches, string kind)
    {
        foreach (Match match in matches)
        {
            markers.Add((match.Index, match.Length, kind));
        }
    }

    /// <summary>Overview is an intent brief — drop a leading Summary / Итоги heading if the model adds one.</summary>
    private static string StripSummaryHeading(string text)
    {
        var trimmed = text.Trim();
        var match = SummaryHeadingRegex().Match(trimmed);
        return match.Success ? trimmed[match.Length..].TrimStart() : trimmed;
    }

    private static string StripMarkdownTables(string text) =>
        MarkdownTableBlock.Replace(text, "\n").Trim();

    private static string TruncateAtSentence(string text, int maxLen)
    {
        if (text.Length <= maxLen)
        {
            return text;
        }

        var slice = text[..maxLen];
        var breakAt = Math.Max(
            slice.LastIndexOf(". ", StringComparison.Ordinal),
            Math.Max(
                slice.LastIndexOf(".\n", StringComparison.Ordinal),
                slice.LastIndexOf('\n')));

        if (breakAt >= maxLen / 2)
        {
            return slice[..(breakAt + 1)].Trim() + "…";
        }

        return slice.TrimEnd() + "…";
    }

    [GeneratedRegex(@"^<<<\s*TITLE\s*>>>|^\s*##\s*TITLE\s*##", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex TitleMarkerRegex();

    [GeneratedRegex(@"^<<<\s*CHAT\s*>>>|^\s*##\s*CHAT\s*##", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex ChatMarkerRegex();

    [GeneratedRegex(@"^<<<\s*OVERVIEW\s*>>>|^\s*##\s*OVERVIEW\s*##", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex OverviewMarkerRegex();

    [GeneratedRegex(@"(?m)(?:^\|[^\n]*\|\s*\n)+", RegexOptions.Multiline)]
    private static partial Regex MarkdownTableBlockRegex();

    [GeneratedRegex(
        @"^(?:#{1,6}\s*)?(?:\*\*)?(?:Summary|Overview summary|Итоги|Сводка|Підсумок|Podsumowanie)(?:\*\*)?\s*:?\s*\r?\n+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SummaryHeadingRegex();
}
