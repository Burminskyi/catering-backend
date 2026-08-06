namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract so Identity startup seeding can trigger inventory catalog seeding
/// without referencing Inventory entities.
/// </summary>
public interface IInventoryDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
