using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class SearchStockBalanceTool : IAssistantTool
{
    /// <summary>
    /// Demo catalog is English; map common uk/ru/pl tokens so "ryżu" / "рис" still find Rice.
    /// </summary>
    private static readonly (string English, string[] Aliases)[] NameAliases =
    [
        ("Rice", ["рис", "ryż", "ryzu", "ryżu", "ryża"]),
        ("Chicken", ["курин", "курк", "kurczak", "kurczaka", "kurczaku"]),
        ("Salmon", ["лосос", "łosoś", "losos", "łososia", "lososia"]),
        ("Beef", ["ялович", "говяд", "wołow", "wolow"]),
        ("Pasta", ["паст", "макарон", "makaron"]),
        ("Potato", ["картоп", "картоф", "ziemniak"]),
        ("Lettuce", ["салат", "салатн", "sałata", "salata"]),
        ("Tomato", ["помідор", "помидор", "pomidor"]),
        ("Onion", ["цибул", "лук", "cebula"]),
        ("Garlic", ["часник", "чеснок", "czosnek"]),
        ("Cream", ["вершк", "сливк", "śmietan", "smietan"]),
        ("Cheese", ["сир", "сыр", "ser"]),
        ("Egg", ["яєц", "яйц", "jajk"]),
        ("Flour", ["борошн", "мук", "mąka", "maka"]),
        ("Oil", ["олі", "масл", "olej"]),
    ];

    private readonly IInventoryReportingQueries _inventory;

    public SearchStockBalanceTool(IInventoryReportingQueries inventory)
    {
        _inventory = inventory;
    }

    public string Name => "search_stock_balance";

    public string Description =>
        "Search current ingredient stock balances in the workspace by partial ingredient name. " +
        "Catalog names are English (Chicken, Rice, Salmon…). Pass English when possible; " +
        "uk/ru/pl synonyms are also matched. Returns quantity, unit, and category. Omit query to list top balances.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] =
                    "Optional partial ingredient name. Prefer English catalog tokens (Rice, Chicken Breast). " +
                    "Translated words (рис, ryż, курица) are accepted too."
            },
            ["limit"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Max rows to return (1-50). Default 20."
            }
        }
    };

    public async Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct)
    {
        var query = (ReportArtifactMapper.ReadString(args, "query") ?? string.Empty).Trim();
        var limit = Math.Clamp(ReportArtifactMapper.ReadInt(args, "limit", 20), 1, 50);

        var balances = await _inventory.GetIngredientBalancesAsync(scope.WorkspaceId, null, ct);

        IEnumerable<IngredientBalanceRow> filtered = balances;
        if (query.Length > 0)
        {
            var tokens = ExpandQueryTokens(query);
            filtered = balances.Where(b =>
                tokens.Any(token => b.Name.Contains(token, StringComparison.OrdinalIgnoreCase)));
        }

        var rows = filtered
            .OrderBy(b => b.Name)
            .Take(limit)
            .Select(b => new Dictionary<string, object?>
            {
                ["ingredientId"] = b.IngredientId,
                ["name"] = b.Name,
                ["category"] = b.Category,
                ["unit"] = b.Unit,
                ["quantity"] = b.Quantity
            })
            .ToList();

        var artifacts = new List<AssistantArtifact>
        {
            new TableArtifact(
                "Stock balance",
                [
                    new ArtifactColumn("name", "Ingredient"),
                    new ArtifactColumn("category", "Category"),
                    new ArtifactColumn("quantity", "Qty"),
                    new ArtifactColumn("unit", "Unit")
                ],
                rows),
            new MetricArtifact("Matched ingredients", rows.Count.ToString())
        };

        return new ToolResult(
            ReportArtifactMapper.BuildRowsFact("Stock balance", [
                new ArtifactColumn("name", "Ingredient"),
                new ArtifactColumn("category", "Category"),
                new ArtifactColumn("quantity", "Qty"),
                new ArtifactColumn("unit", "Unit")
            ], rows),
            artifacts);
    }

    private static List<string> ExpandQueryTokens(string query)
    {
        var tokens = new List<string> { query };
        foreach (var (english, aliases) in NameAliases)
        {
            if (query.Contains(english, StringComparison.OrdinalIgnoreCase)
                || aliases.Any(a =>
                    query.Contains(a, StringComparison.OrdinalIgnoreCase)
                    || a.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                tokens.Add(english);
            }
        }

        return tokens;
    }
}
