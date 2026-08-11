using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public sealed class WorkspaceManagerProvisioner : IWorkspaceManagerProvisioner
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public WorkspaceManagerProvisioner(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task ProvisionCateringManagerAsync(
        Guid workspaceId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        await EnsureEmailAvailableAsync(normalizedEmail, excludeUserId: null, cancellationToken);

        var username = normalizedEmail.Split('@')[0];
        await EnsureUsernameAvailableAsync(username, workspaceId, excludeUserId: null, cancellationToken);

        var manager = new User
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Username = username,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(password),
            FirstName = "Workspace",
            LastName = "Admin",
            Role = StaffRole.WorkspaceAdmin,
            IsActive = true,
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
            .Where(u => u.WorkspaceId == workspaceId && u.Role == StaffRole.WorkspaceAdmin)
            .OrderBy(u => u.Username)
            .FirstOrDefaultAsync(cancellationToken);

        if (manager is null)
        {
            throw new KeyNotFoundException(
                $"No WorkspaceAdmin found for workspace '{workspaceId}'.");
        }

        var normalizedEmail = NormalizeEmail(email);
        await EnsureEmailAvailableAsync(normalizedEmail, excludeUserId: manager.Id, cancellationToken);

        var username = normalizedEmail.Split('@')[0];
        if (!string.Equals(manager.Username, username, StringComparison.Ordinal))
        {
            await EnsureUsernameAvailableAsync(username, workspaceId, excludeUserId: manager.Id, cancellationToken);
            manager.Username = username;
        }

        manager.Email = normalizedEmail;
        manager.PasswordHash = _passwordHasher.Hash(password);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task EnsureEmailAvailableAsync(
        string normalizedEmail,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<User>().Where(u => u.Email == normalizedEmail);

        if (excludeUserId is Guid userId)
        {
            query = query.Where(u => u.Id != userId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Email '{normalizedEmail}' is already registered.");
        }
    }

    private async Task EnsureUsernameAvailableAsync(
        string username,
        Guid workspaceId,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<User>()
            .Where(u => u.WorkspaceId == workspaceId && u.Username == username);

        if (excludeUserId is Guid userId)
        {
            query = query.Where(u => u.Id != userId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Username '{username}' is already taken in this workspace.");
        }
    }
}
