namespace CateringSaaS.Modules.Inventory.Domain.Models;

/// <summary>
/// FIFO stock batch for a workspace ingredient receipt.
/// </summary>
public class StockBatch
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    public decimal InitialQuantity { get; set; }

    public decimal CurrentQuantity { get; set; }

    /// <summary>
    /// Total cost of the entire batch (not per-unit). Unit cost = CostPrice / InitialQuantity when InitialQuantity &gt; 0.
    /// </summary>
    public decimal CostPrice { get; set; }

    public DateTime ReceivedAt { get; set; }
}
