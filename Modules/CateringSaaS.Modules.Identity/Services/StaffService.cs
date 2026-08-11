using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Modules.Identity.Services;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public interface IStaffService
{
    Task<IReadOnlyList<StaffMemberResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StaffMemberResponse> CreateAsync(CreateStaffMemberRequest request, CancellationToken cancellationToken = default);

    Task<StaffMemberResponse> UpdateAsync(Guid id, UpdateStaffMemberRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class StaffService : IStaffService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public StaffService(
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

    public async Task<IReadOnlyList<StaffMemberResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        return await _dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => u.WorkspaceId == workspaceId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => ToStaffResponse(u))
            .ToListAsync(cancellationToken);
    }

    public async Task<StaffMemberResponse> CreateAsync(
        CreateStaffMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var username = NormalizeUsername(request.Username);
        var role = ParseStaffRole(request.Role);

        await EnsureUsernameAvailableAsync(username, workspaceId, excludeUserId: null, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await EnsureEmailAvailableAsync(request.Email, excludeUserId: null, cancellationToken);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Username = username,
            Email = NormalizeEmail(request.Email),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            AvatarUrl = request.AvatarUrl?.Trim(),
            Role = role,
            IsActive = true,
            CompanyId = null
        };

        await _dbContext.Set<User>().AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToStaffResponse(user);
    }

    public async Task<StaffMemberResponse> UpdateAsync(
        Guid id,
        UpdateStaffMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var user = await GetWorkspaceUserOrThrowAsync(id, workspaceId, cancellationToken);

        var username = NormalizeUsername(request.Username);
        if (!string.Equals(user.Username, username, StringComparison.Ordinal))
        {
            await EnsureUsernameAvailableAsync(username, workspaceId, excludeUserId: id, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await EnsureEmailAvailableAsync(request.Email, excludeUserId: id, cancellationToken);
        }

        user.Username = username;
        user.Email = NormalizeEmail(request.Email);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.AvatarUrl = request.AvatarUrl?.Trim();
        user.Role = ParseStaffRole(request.Role);
        user.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToStaffResponse(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        if (_currentUser.UserId == id)
        {
            throw new IdentityServiceException(
                "You cannot deactivate your own account.",
                StatusCodes.Status409Conflict);
        }

        var user = await GetWorkspaceUserOrThrowAsync(id, workspaceId, cancellationToken);
        user.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetWorkspaceUserOrThrowAsync(
        Guid id,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == id && u.WorkspaceId == workspaceId, cancellationToken);

        if (user is null)
        {
            throw new IdentityServiceException(
                $"Staff member '{id}' was not found in this workspace.",
                StatusCodes.Status404NotFound);
        }

        return user;
    }

    private async Task EnsureUsernameAvailableAsync(
        string username,
        Guid workspaceId,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<User>()
            .Where(u => u.WorkspaceId == workspaceId && u.Username == username);

        if (excludeUserId is Guid id)
        {
            query = query.Where(u => u.Id != id);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new IdentityServiceException(
                $"Username '{username}' is already taken in this workspace.",
                StatusCodes.Status409Conflict);
        }
    }

    private async Task EnsureEmailAvailableAsync(
        string? email,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var normalized = NormalizeEmail(email)!;
        var query = _dbContext.Set<User>().Where(u => u.Email == normalized);

        if (excludeUserId is Guid id)
        {
            query = query.Where(u => u.Id != id);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new IdentityServiceException(
                $"Email '{normalized}' is already registered.",
                StatusCodes.Status409Conflict);
        }
    }

    private static StaffRole ParseStaffRole(string role)
    {
        if (!Enum.TryParse<StaffRole>(role, ignoreCase: true, out var parsed)
            || parsed is StaffRole.SuperAdmin
                or StaffRole.ClientAdmin
                or StaffRole.ClientEmployee)
        {
            throw new IdentityServiceException($"Invalid staff role '{role}'.");
        }

        return parsed;
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

    private static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static StaffMemberResponse ToStaffResponse(User user) =>
        new(
            user.Id,
            user.WorkspaceId!.Value,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.IsActive);
}
