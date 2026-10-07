namespace CateringSaaS.Modules.Tenants.DTOs;

public sealed record ClientCompanyResponse(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    bool IsActive,
    string OrderCadence,
    string? Address,
    string? ContactPhone,
    string? ContactName);

public sealed record CreateClientCompanyRequest(
    string Name,
    string? OrderCadence,
    string? Address,
    string? ContactPhone,
    string? ContactName,
    string? AdminUsername,
    string? AdminPassword,
    string? AdminEmail,
    string? AdminFirstName,
    string? AdminLastName);
