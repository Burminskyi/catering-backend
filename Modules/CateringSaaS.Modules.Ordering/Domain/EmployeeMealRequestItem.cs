namespace CateringSaaS.Modules.Ordering.Domain;

public class EmployeeMealRequestItem
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid RequestId { get; set; }

    public EmployeeMealRequest Request { get; set; } = null!;

    /// <summary>References Menu MenuItem by id — no cross-module EF nav.</summary>
    public Guid MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }
}
