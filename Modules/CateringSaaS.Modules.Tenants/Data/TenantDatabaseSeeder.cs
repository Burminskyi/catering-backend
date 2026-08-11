using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Modules.Tenants.Data;

public sealed class TenantDatabaseSeeder : ITenantDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TenantDatabaseSeeder> _logger;

    public TenantDatabaseSeeder(AppDbContext dbContext, ILogger<TenantDatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var workspaces = _dbContext.Set<Workspace>();

        if (!await workspaces.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding mock workspace for frontend testing...");

            await workspaces.AddAsync(
                new Workspace
                {
                    Id = DevelopmentSeedIds.MockWorkspaceId,
                    Name = "Romashka Catering",
                    Subdomain = DevelopmentSeedIds.MockSubdomain,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
                    PlanType = "Professional"
                },
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var clients = _dbContext.Set<ClientCompany>();

        if (!await clients.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding mock client company for frontend testing...");

            await clients.AddAsync(
                new ClientCompany
                {
                    Id = DevelopmentSeedIds.MockClientCompanyId,
                    WorkspaceId = DevelopmentSeedIds.MockWorkspaceId,
                    Name = "Office Corp",
                    IsActive = true
                },
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
