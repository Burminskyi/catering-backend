namespace CateringSaaS.Modules.Menu.Domain;

public class MenuItem
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid MenuDayId { get; set; }

    public MenuDay MenuDay { get; set; } = null!;

    public Guid DishId { get; set; }

    public Dish Dish { get; set; } = null!;

    public decimal SellingPrice { get; set; }
}
