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

    public async Task<IReadOnlyDictionary<Guid, ClientCompanyContact>> GetContactsAsync(
        Guid workspaceId,
        IEnumerable<Guid> clientCompanyIds,
        CancellationToken cancellationToken = default)
    {
        var ids = clientCompanyIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ClientCompanyContact>();
        }

        var rows = await _dbContext.Set<ClientCompany>()
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId && ids.Contains(c.Id))
            .Select(c => new ClientCompanyContact(c.Id, c.Name, c.Address, c.ContactPhone, c.ContactName))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(c => c.Id);
    }
}
