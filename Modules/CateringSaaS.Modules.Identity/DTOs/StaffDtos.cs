namespace CateringSaaS.Modules.Identity.DTOs;

public sealed record StaffMemberResponse(
    Guid Id,
    Guid WorkspaceId,
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string Role,
    bool IsActive);

public sealed record CreateStaffMemberRequest(
    string Username,
    string Password,
    string? Email,
    string FirstName,
    string LastName,
    string Role,
    string? AvatarUrl);

public sealed record UpdateStaffMemberRequest(
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    string? AvatarUrl);

public sealed record ProfileResponse(
    Guid Id,
    Guid? WorkspaceId,
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string Role,
    bool IsActive);

public sealed record UpdateProfileRequest(
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    string? AvatarUrl);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
