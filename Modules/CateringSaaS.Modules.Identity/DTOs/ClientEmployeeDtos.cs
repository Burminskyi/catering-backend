namespace CateringSaaS.Modules.Identity.DTOs;

public sealed record ClientEmployeeResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ClientCompanyId,
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive);

public sealed record CreateClientEmployeeRequest(
    string Username,
    string Password,
    string? Email,
    string FirstName,
    string LastName);
