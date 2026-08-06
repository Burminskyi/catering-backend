using CateringSaaS.Modules.Inventory.Domain.Enums;

namespace CateringSaaS.Modules.Inventory.Domain.Models;

/// <summary>
/// Global system ingredient when <see cref="WorkspaceId"/> is null;
/// tenant-specific ingredient when <see cref="WorkspaceId"/> is set.
/// </summary>
public class Ingredient
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public IngredientCategory Category { get; set; }

    public UnitOfMeasure BaseUnit { get; set; }

    /// <summary>
    /// Null = global shared catalog entry available to all workspaces.
    /// </summary>
    public Guid? WorkspaceId { get; set; }

    public ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
