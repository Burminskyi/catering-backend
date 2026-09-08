namespace CateringSaaS.Modules.Ordering.Domain;

public class EmployeeMealRequestItem
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid RequestId { get; set; }

    public EmployeeMealRequest Request { get; set; } = null!;

    /// <summary>References Menu MenuItem by id — no cross-module EF nav.</summary>
    public Guid MenuItemId { get; set; }

    /// <summary>Optional dish id snapshot from menu at request time.</summary>
    public Guid? DishId { get; set; }

    /// <summary>Dish name snapshot at request time (stable if dish is renamed later).</summary>
    public string DishName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }
}
