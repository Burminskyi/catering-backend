namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Seeds realistic operational demo data (clients, dishes, menus, orders, stock, reviews)
/// for the default development workspace so reports and Overview can be tested.
/// </summary>
public interface IOperationalDemoSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
