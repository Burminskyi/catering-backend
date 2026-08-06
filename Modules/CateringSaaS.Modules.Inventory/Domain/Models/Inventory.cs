namespace CateringSaaS.Modules.Inventory.Domain.Models;

/// <summary>
/// Aggregated on-hand quantity for an ingredient within a workspace (sum of FIFO batches).
/// </summary>
public class Inventory
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    public decimal TotalQuantity { get; set; }
}
