using System.Globalization;
using System.Text;

namespace CateringSaaS.Modules.Inventory.Services;

internal static class IngredientCsvParser
{
    internal sealed record Row(string Name, string Unit, decimal CostPerUnit);

    public static async Task<IReadOnlyList<Row>> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return Parse(content);
    }

    public static IReadOnlyList<Row> Parse(string content)
    {
        var lines = content
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
        {
            return [];
        }

        var delimiter = DetectDelimiter(lines[0]);
        var startIndex = 0;
        var nameIndex = 0;
        var unitIndex = 1;
        var costIndex = 2;

        var headerCells = SplitCsvLine(lines[0], delimiter);
        if (LooksLikeHeader(headerCells))
        {
            nameIndex = IndexOf(headerCells, "name", "ingredient", "назва");
            unitIndex = IndexOf(headerCells, "unit", "baseunit", "одиниця");
            costIndex = IndexOf(headerCells, "costperunit", "cost", "price", "ціна");
            if (nameIndex < 0) nameIndex = 0;
            if (unitIndex < 0) unitIndex = headerCells.Count > 1 ? 1 : 0;
            if (costIndex < 0) costIndex = headerCells.Count > 2 ? 2 : -1;
            startIndex = 1;
        }

        var rows = new List<Row>();
        for (var i = startIndex; i < lines.Length; i++)
        {
            var cells = SplitCsvLine(lines[i], delimiter);
            var name = ReadCell(cells, nameIndex);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var unit = ReadCell(cells, unitIndex);
            var costText = ReadCell(cells, costIndex);
            decimal cost = 0;
            if (!string.IsNullOrWhiteSpace(costText) && (!TryParseDecimal(costText, out cost) || cost < 0))
            {
                continue;
            }

            rows.Add(new Row(name.Trim(), unit.Trim(), cost));
        }

        return rows;
    }

    private static bool LooksLikeHeader(IReadOnlyList<string> cells)
    {
        var joined = string.Join(' ', cells).ToLowerInvariant();
        return joined.Contains("name") || joined.Contains("назва") || joined.Contains("unit") || joined.Contains("cost");
    }

    private static char DetectDelimiter(string header)
    {
        var commas = header.Count(c => c == ',');
        var semicolons = header.Count(c => c == ';');
        return semicolons > commas ? ';' : ',';
    }

    private static int IndexOf(IReadOnlyList<string> cells, params string[] aliases)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            var normalized = cells[i].Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
            if (aliases.Contains(normalized))
            {
                return i;
            }
        }

        return -1;
    }

    private static string ReadCell(IReadOnlyList<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
        {
            return string.Empty;
        }

        return cells[index];
    }

    private static bool TryParseDecimal(string value, out decimal number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            normalized = normalized.Replace(',', '.');
        }
        else
        {
            normalized = normalized.Replace(",", string.Empty);
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
    }

    private static List<string> SplitCsvLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == delimiter && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        cells.Add(current.ToString());
        return cells;
    }
}
