namespace CateringSaaS.Modules.Tenants.DTOs;

public sealed record ClientCompanyResponse(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    bool IsActive);

public sealed record CreateClientCompanyRequest(
    string Name,
    string? AdminUsername,
    string? AdminPassword,
    string? AdminEmail,
    string? AdminFirstName,
    string? AdminLastName);
