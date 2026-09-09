namespace CateringSaaS.Modules.Ordering.Domain;

public class Order
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid ClientCompanyId { get; set; }

    /// <summary>References Identity User by id (no cross-module EF nav).</summary>
    public Guid PlacedByUserId { get; set; }

    public DateOnly TargetDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    /// <summary>Assigned delivery driver (Identity User id). Null until assigned.</summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// Set when FIFO stock was consumed for this order (typically at InProduction).
    /// Prevents double deduction on repeated status transitions.
    /// </summary>
    public DateTime? StockConsumedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
