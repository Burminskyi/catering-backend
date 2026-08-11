using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public sealed class ClientAdminProvisioner : IClientAdminProvisioner
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ClientAdminProvisioner(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task ProvisionClientAdminAsync(
        Guid workspaceId,
        Guid clientCompanyId,
        string username,
        string password,
        string? email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();

        var usernameTaken = await _dbContext.Set<User>()
            .AnyAsync(
                u => u.WorkspaceId == workspaceId && u.Username == normalizedUsername,
                cancellationToken);

        if (usernameTaken)
        {
            throw new InvalidOperationException(
                $"Username '{normalizedUsername}' is already taken in this workspace.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var emailTaken = await _dbContext.Set<User>()
                .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (emailTaken)
            {
                throw new InvalidOperationException($"Email '{normalizedEmail}' is already registered.");
            }
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            Username = normalizedUsername,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(password),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = StaffRole.ClientAdmin,
            IsActive = true,
            CompanyId = null
        };

        await _dbContext.Set<User>().AddAsync(admin, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
