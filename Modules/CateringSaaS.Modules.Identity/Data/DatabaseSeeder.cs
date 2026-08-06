using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Modules.Identity.Data;

public sealed class DatabaseSeeder
{
    public static readonly Guid MockWorkspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid MockCompanyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string DefaultPassword = "admin123";

    private readonly AppDbContext _dbContext;
    private readonly IInventoryDataSeeder _inventoryDataSeeder;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext dbContext,
        IInventoryDataSeeder inventoryDataSeeder,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _inventoryDataSeeder = inventoryDataSeeder;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        var users = _dbContext.Set<User>();
        if (!await users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding mock identity users for frontend testing...");

            var seedUsers = new[]
            {
                CreateUser("superadmin@test.com", AppRole.SuperAdmin, workspaceId: null, companyId: null),
                CreateUser("manager@test.com", AppRole.CateringManager, MockWorkspaceId, companyId: null),
                CreateUser("staff@test.com", AppRole.CateringStaff, MockWorkspaceId, companyId: null),
                CreateUser("driver@test.com", AppRole.CateringDriver, MockWorkspaceId, companyId: null),
                CreateUser("officemanager@test.com", AppRole.OfficeManager, MockWorkspaceId, MockCompanyId),
                CreateUser("employee@test.com", AppRole.OfficeEmployee, MockWorkspaceId, MockCompanyId)
            };

            await users.AddRangeAsync(seedUsers, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Seeded {UserCount} users. WorkspaceId={WorkspaceId}, CompanyId={CompanyId}",
                seedUsers.Length,
                MockWorkspaceId,
                MockCompanyId);
        }

        // Global shared ingredient catalog (Inventory module) — independent of user seed.
        await _inventoryDataSeeder.SeedAsync(cancellationToken);
    }

    private static User CreateUser(string email, AppRole role, Guid? workspaceId, Guid? companyId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            // MVP: store plain text for simple comparison during login testing
            PasswordHash = DefaultPassword,
            Role = role,
            WorkspaceId = workspaceId,
            CompanyId = companyId
        };
}
