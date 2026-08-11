namespace CateringSaaS.Modules.Menu.Domain;

public class Dish
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DishCategory Category { get; set; }

    /// <summary>Final portion size in grams.</summary>
    public int OutputWeight { get; set; }

    public string? Instructions { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DishIngredient> Ingredients { get; set; } = new List<DishIngredient>();
}
