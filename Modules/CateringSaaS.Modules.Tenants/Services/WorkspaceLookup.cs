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
}
