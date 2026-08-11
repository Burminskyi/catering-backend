namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract so Identity startup seeding can trigger tenant seed data
/// without referencing Tenants entities.
/// </summary>
public interface ITenantDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
