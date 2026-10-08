using System.Text;

namespace CateringSaaS.Modules.Assistant.Services;

internal static class ConversationTitle
{
    private const int MaxLength = 100;

    public static string FromFirstMessage(string message) =>
        Normalize(message) ?? "New chat";

    /// <summary>Cleans an LLM- or user-provided title. Returns null when empty.</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var collapsed = CollapseWhitespace(raw.Trim());
        // Drop wrapping quotes / markdown bold the model sometimes adds.
        collapsed = collapsed.Trim('"', '\'', '*', '`');
        if (collapsed.Length == 0)
        {
            return null;
        }

        if (collapsed.Length <= MaxLength)
        {
            return collapsed;
        }

        var cut = collapsed[..MaxLength].TrimEnd();
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace >= MaxLength / 2)
        {
            cut = cut[..lastSpace];
        }

        return cut + "…";
    }

    public static bool LooksLikeCopiedUserMessage(string? title, string? userMessage)
    {
        var normalizedTitle = Normalize(title);
        var fromUser = Normalize(userMessage);
        if (normalizedTitle is null || fromUser is null)
        {
            return false;
        }

        return string.Equals(normalizedTitle, fromUser, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalizedTitle, FromFirstMessage(userMessage ?? string.Empty), StringComparison.OrdinalIgnoreCase);
    }

    private static string CollapseWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && sb.Length > 0)
            {
                sb.Append(' ');
            }

            pendingSpace = false;
            sb.Append(ch);
        }

        return sb.ToString();
    }
}
