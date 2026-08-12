namespace CateringSaaS.Modules.Ordering.Domain;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    InProduction = 2,
    Delivered = 3,
    Cancelled = 4
}
