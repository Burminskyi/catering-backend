using System.Text;

namespace CateringSaaS.Modules.Knowledge.Services;

/// <summary>
/// Sliding-window chunker targeting ~400–500 tokens (~1500 chars) with ~12% overlap.
/// </summary>
public sealed class TextChunker : ITextChunker
{
    public const int TargetChars = 1500;
    public const int OverlapChars = 180; // ~12% of 1500
    public const int MinChars = 200;

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0)
        {
            return Array.Empty<TextChunk>();
        }

        if (normalized.Length <= TargetChars)
        {
            return
            [
                new TextChunk(0, normalized, EstimateTokens(normalized))
            ];
        }

        var chunks = new List<TextChunk>();
        var start = 0;
        var index = 0;

        while (start < normalized.Length)
        {
            var remaining = normalized.Length - start;
            var length = Math.Min(TargetChars, remaining);
            var end = start + length;

            if (end < normalized.Length)
            {
                end = FindBreak(normalized, start, end);
                length = Math.Max(MinChars, end - start);
                end = start + length;
            }

            var slice = normalized[start..end].Trim();
            if (slice.Length > 0)
            {
                chunks.Add(new TextChunk(index++, slice, EstimateTokens(slice)));
            }

            if (end >= normalized.Length)
            {
                break;
            }

            var next = end - OverlapChars;
            start = Math.Max(start + 1, next);
        }

        return chunks;
    }

    private static int FindBreak(string text, int start, int proposedEnd)
    {
        // Prefer paragraph, then sentence, then whitespace near the window end.
        var window = text[start..proposedEnd];
        var paragraph = window.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (paragraph >= MinChars)
        {
            return start + paragraph;
        }

        var sentence = LastIndexOfAny(window, [". ", "! ", "? ", ".\n", "!\n", "?\n"]);
        if (sentence >= MinChars)
        {
            return start + sentence + 1;
        }

        var space = window.LastIndexOf(' ');
        if (space >= MinChars)
        {
            return start + space;
        }

        return proposedEnd;
    }

    private static int LastIndexOfAny(string text, string[] markers)
    {
        var best = -1;
        foreach (var marker in markers)
        {
            var idx = text.LastIndexOf(marker, StringComparison.Ordinal);
            if (idx > best)
            {
                best = idx;
            }
        }

        return best;
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var previousWasSpace = false;

        foreach (var ch in text.Replace("\r\n", "\n", StringComparison.Ordinal))
        {
            if (ch is '\t')
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            if (ch == ' ')
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            builder.Append(ch);
            previousWasSpace = false;
        }

        return builder.ToString().Trim();
    }

    /// <summary>Rough multilingual token estimate (~4 chars/token).</summary>
    public static int EstimateTokens(string text) =>
        Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
}
