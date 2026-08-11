using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Identity.Services;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Modules.Identity.Data;

public sealed class DatabaseSeeder
{
    public static readonly Guid MockWorkspaceId = DevelopmentSeedIds.MockWorkspaceId;
    public static readonly Guid MockCompanyId = DevelopmentSeedIds.MockClientCompanyId;

    private const string DefaultPassword = "admin123";

    private readonly AppDbContext _dbContext;
    private readonly ITenantDataSeeder _tenantDataSeeder;
    private readonly IInventoryDataSeeder _inventoryDataSeeder;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext dbContext,
        ITenantDataSeeder tenantDataSeeder,
        IInventoryDataSeeder inventoryDataSeeder,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _tenantDataSeeder = tenantDataSeeder;
        _inventoryDataSeeder = inventoryDataSeeder;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
        await _tenantDataSeeder.SeedAsync(cancellationToken);

        var users = _dbContext.Set<User>();
        if (!await users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding mock identity users for frontend testing...");

            var passwordHash = _passwordHasher.Hash(DefaultPassword);

            var seedUsers = new[]
            {
                CreateUser("superadmin", "superadmin@test.com", "Super", "Admin", StaffRole.SuperAdmin, null, null, null, passwordHash),
                CreateUser("manager", "manager@test.com", "Catering", "Manager", StaffRole.WorkspaceAdmin, MockWorkspaceId, null, null, passwordHash),
                CreateUser("staff", null, "Kitchen", "Staff", StaffRole.Staff, MockWorkspaceId, null, null, passwordHash),
                CreateUser("driver", null, "Delivery", "Driver", StaffRole.Driver, MockWorkspaceId, null, null, passwordHash),
                CreateUser("officemanager", "officemanager@test.com", "Office", "Manager", StaffRole.ClientAdmin, MockWorkspaceId, MockCompanyId, MockCompanyId, passwordHash),
                CreateUser("employee", "employee@test.com", "Office", "Employee", StaffRole.ClientEmployee, MockWorkspaceId, MockCompanyId, MockCompanyId, passwordHash)
            };

            await users.AddRangeAsync(seedUsers, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Seeded {UserCount} users. WorkspaceId={WorkspaceId}, Subdomain={Subdomain}, ClientCompanyId={ClientCompanyId}",
                seedUsers.Length,
                MockWorkspaceId,
                DevelopmentSeedIds.MockSubdomain,
                MockCompanyId);
        }

        await _inventoryDataSeeder.SeedAsync(cancellationToken);
    }

    private static User CreateUser(
        string username,
        string? email,
        string firstName,
        string lastName,
        StaffRole role,
        Guid? workspaceId,
        Guid? clientCompanyId,
        Guid? companyId,
        string passwordHash) =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            CompanyId = companyId,
            IsActive = true
        };
}
