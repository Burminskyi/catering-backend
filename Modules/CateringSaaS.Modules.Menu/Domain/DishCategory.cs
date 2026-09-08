namespace CateringSaaS.Modules.Menu.Domain;

/// <summary>
/// Dish tech-card categories. Canonical codes match catering-web DISH_CATEGORIES / PWA filters:
/// Soup, Salad, Main, Side, Dessert, Drink.
/// </summary>
public enum DishCategory
{
    Soup = 0,
    Main = 1,
    Salad = 2,
    Dessert = 3,
    Drink = 4,
    Side = 5
}
