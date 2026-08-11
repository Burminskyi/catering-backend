using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public interface IClientEmployeeService
{
    Task<IReadOnlyList<ClientEmployeeResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ClientEmployeeResponse> CreateAsync(
        CreateClientEmployeeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ClientEmployeeService : IClientEmployeeService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ClientEmployeeService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<ClientEmployeeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();

        return await _dbContext.Set<User>()
            .AsNoTracking()
            .Where(u =>
                u.WorkspaceId == workspaceId
                && u.ClientCompanyId == clientCompanyId
                && u.Role == StaffRole.ClientEmployee)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => ToResponse(u))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientEmployeeResponse> CreateAsync(
        CreateClientEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireClientAdminRole();

        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var username = NormalizeUsername(request.Username);

        if (string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new IdentityServiceException("Username, password, first name, and last name are required.");
        }

        await EnsureUsernameAvailableAsync(username, workspaceId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await EnsureEmailAvailableAsync(request.Email, cancellationToken);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            Username = username,
            Email = NormalizeEmail(request.Email),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = StaffRole.ClientEmployee,
            IsActive = true,
            CompanyId = null
        };

        await _dbContext.Set<User>().AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    private void RequireClientAdminRole()
    {
        if (!string.Equals(_currentUser.Role, StaffRole.ClientAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new IdentityServiceException(
                "Only ClientAdmin can manage client employees.",
                StatusCodes.Status403Forbidden);
        }
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new IdentityServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private Guid RequireClientCompany()
    {
        if (_currentUser.ClientCompanyId is not Guid clientCompanyId || clientCompanyId == Guid.Empty)
        {
            throw new IdentityServiceException(
                "Client company context is required.",
                StatusCodes.Status400BadRequest);
        }

        return clientCompanyId;
    }

    private async Task EnsureUsernameAvailableAsync(
        string username,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.Set<User>()
                .AnyAsync(u => u.WorkspaceId == workspaceId && u.Username == username, cancellationToken))
        {
            throw new IdentityServiceException(
                $"Username '{username}' is already taken in this workspace.",
                StatusCodes.Status409Conflict);
        }
    }

    private async Task EnsureEmailAvailableAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var normalized = NormalizeEmail(email)!;
        if (await _dbContext.Set<User>().AnyAsync(u => u.Email == normalized, cancellationToken))
        {
            throw new IdentityServiceException(
                $"Email '{normalized}' is already registered.",
                StatusCodes.Status409Conflict);
        }
    }

    private static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static ClientEmployeeResponse ToResponse(User user) =>
        new(
            user.Id,
            user.WorkspaceId!.Value,
            user.ClientCompanyId!.Value,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive);
}
