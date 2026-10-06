using System.Text.Json.Nodes;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Services;
using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Modules.Assistant.Tools;

public sealed class SearchStockBalanceTool : IAssistantTool
{
    private readonly IInventoryReportingQueries _inventory;

    public SearchStockBalanceTool(IInventoryReportingQueries inventory)
    {
        _inventory = inventory;
    }

    public string Name => "search_stock_balance";

    public string Description =>
        "Search current ingredient stock balances in the workspace by partial ingredient name. " +
        "Returns quantity, unit, and category. Omit query to list top balances.";

    public JsonObject ParametersSchema { get; } = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional partial ingredient name filter (e.g. Chicken, Rice)."
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
            filtered = balances.Where(b =>
                b.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
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

        return new ToolResult(new { count = rows.Count, items = rows }, artifacts);
    }
}
