namespace CateringSaaS.Modules.Ordering.Domain;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    /// <summary>References Menu MenuItem by id — no cross-module EF nav.</summary>
    public Guid MenuItemId { get; set; }

    /// <summary>Dish id snapshot at order time (for stock consume / UI).</summary>
    public Guid? DishId { get; set; }

    /// <summary>Dish name snapshot at order time.</summary>
    public string DishName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }
}
