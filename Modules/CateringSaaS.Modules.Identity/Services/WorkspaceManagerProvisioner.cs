using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public sealed class WorkspaceManagerProvisioner : IWorkspaceManagerProvisioner
{
    private readonly AppDbContext _dbContext;

    public WorkspaceManagerProvisioner(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ProvisionCateringManagerAsync(
        Guid workspaceId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        await EnsureEmailAvailableAsync(normalizedEmail, excludeUserId: null, cancellationToken);

        var manager = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            // MVP: plain-text password storage (same as seed/login)
            PasswordHash = password,
            Role = AppRole.CateringManager,
            WorkspaceId = workspaceId,
            CompanyId = null
        };

        await _dbContext.Set<User>().AddAsync(manager, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePrimaryCateringManagerAsync(
        Guid workspaceId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var manager = await _dbContext.Set<User>()
            .Where(u => u.WorkspaceId == workspaceId && u.Role == AppRole.CateringManager)
            .OrderBy(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (manager is null)
        {
            throw new KeyNotFoundException(
                $"No CateringManager found for workspace '{workspaceId}'.");
        }

        var normalizedEmail = NormalizeEmail(email);
        await EnsureEmailAvailableAsync(normalizedEmail, excludeUserId: manager.Id, cancellationToken);

        manager.Email = normalizedEmail;
        manager.PasswordHash = password;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task EnsureEmailAvailableAsync(
        string normalizedEmail,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<User>().Where(u => u.Email.ToLower() == normalizedEmail);

        if (excludeUserId is Guid userId)
        {
            query = query.Where(u => u.Id != userId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Email '{normalizedEmail}' is already registered.");
        }
    }
}
