using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Identity.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Identity.Services;

public interface IProfileService
{
    Task<ProfileResponse> GetAsync(CancellationToken cancellationToken = default);

    Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileService(
        AppDbContext dbContext,
        ICurrentUserContext currentUser,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<ProfileResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserOrThrowAsync(cancellationToken);
        return ToProfileResponse(user);
    }

    public async Task<ProfileResponse> UpdateAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserOrThrowAsync(cancellationToken);
        var username = request.Username.Trim().ToLowerInvariant();

        if (!string.Equals(user.Username, username, StringComparison.Ordinal))
        {
            await EnsureUsernameAvailableAsync(user, username, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailTaken = await _dbContext.Set<User>()
                .AnyAsync(
                    u => u.Email == normalizedEmail && u.Id != user.Id,
                    cancellationToken);

            if (emailTaken)
            {
                throw new IdentityServiceException(
                    $"Email '{normalizedEmail}' is already registered.",
                    StatusCodes.Status409Conflict);
            }

            user.Email = normalizedEmail;
        }
        else
        {
            user.Email = null;
        }

        user.Username = username;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.AvatarUrl = request.AvatarUrl?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToProfileResponse(user);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserOrThrowAsync(cancellationToken);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new IdentityServiceException(
                "Current password is incorrect.",
                StatusCodes.Status400BadRequest);
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUsernameAvailableAsync(
        User user,
        string username,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<User>().Where(u => u.Username == username && u.Id != user.Id);

        if (user.WorkspaceId is Guid workspaceId)
        {
            query = query.Where(u => u.WorkspaceId == workspaceId);
        }
        else
        {
            query = query.Where(u => u.WorkspaceId == null);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new IdentityServiceException(
                $"Username '{username}' is already taken.",
                StatusCodes.Status409Conflict);
        }
    }

    private async Task<User> GetCurrentUserOrThrowAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            throw new IdentityServiceException("Authentication is required.", StatusCodes.Status401Unauthorized);
        }

        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
        {
            throw new IdentityServiceException("User profile was not found.", StatusCodes.Status404NotFound);
        }

        if (!user.IsActive)
        {
            throw new IdentityServiceException("User account is inactive.", StatusCodes.Status403Forbidden);
        }

        return user;
    }

    private static ProfileResponse ToProfileResponse(User user) =>
        new(
            user.Id,
            user.WorkspaceId,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.IsActive);
}
