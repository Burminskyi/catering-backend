namespace CateringSaaS.Modules.Menu.Domain;

public class DishIngredient
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid DishId { get; set; }

    public Dish Dish { get; set; } = null!;

    /// <summary>References Inventory Ingredient by id (no cross-module EF nav).</summary>
    public Guid IngredientId { get; set; }

    /// <summary>Amount in the ingredient's BaseUnit.</summary>
    public decimal Quantity { get; set; }
}
