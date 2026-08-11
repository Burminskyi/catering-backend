using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Services;

public sealed class WorkspaceLookup : IWorkspaceLookup
{
    private readonly AppDbContext _dbContext;

    public WorkspaceLookup(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        _dbContext.Set<Workspace>().AnyAsync(w => w.Id == workspaceId, cancellationToken);

    public async Task<Guid?> ResolveWorkspaceIdBySubdomainAsync(
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        var normalized = subdomain.Trim().ToLowerInvariant();

        return await _dbContext.Set<Workspace>()
            .AsNoTracking()
            .Where(w => w.Subdomain == normalized && w.IsActive)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
