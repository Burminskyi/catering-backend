using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Services;

public sealed class ClientCompanyLookup : IClientCompanyLookup
{
    private readonly AppDbContext _dbContext;

    public ClientCompanyLookup(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsInWorkspaceAsync(
        Guid clientCompanyId,
        Guid workspaceId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<ClientCompany>()
            .AsNoTracking()
            .AnyAsync(
                c => c.Id == clientCompanyId && c.WorkspaceId == workspaceId && c.IsActive,
                cancellationToken);
}
