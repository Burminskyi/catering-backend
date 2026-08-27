using CateringSaaS.Modules.Inventory.Domain.Enums;

namespace CateringSaaS.Modules.Inventory.Domain.Models;

/// <summary>
/// Immutable ledger row for every stock change (purchase, consume, adjustment).
/// </summary>
public class InventoryMovement
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    public InventoryMovementType Type { get; set; }

    /// <summary>Absolute quantity moved (always &gt; 0).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Signed delta: +Purchase, −Consume/−Adjustment.</summary>
    public decimal SignedQuantity { get; set; }

    public decimal TotalCost { get; set; }

    public required string Source { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
