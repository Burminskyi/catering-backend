using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public sealed class UserDisplayLookup : IUserDisplayLookup
{
    private readonly AppDbContext _dbContext;

    public UserDisplayLookup(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await _dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Username,
                u.Email
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            u => u.Id,
            u =>
            {
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    return fullName;
                }

                if (!string.IsNullOrWhiteSpace(u.Username))
                {
                    return u.Username;
                }

                return u.Email ?? u.Id.ToString();
            });
    }
}
