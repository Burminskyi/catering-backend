namespace CateringSaaS.Modules.Tenants.DTOs;

public sealed record WorkspaceBrandingResponse(
    Guid Id,
    string Name,
    string? LogoUrl,
    string Subdomain);

public sealed record UpdateWorkspaceBrandingRequest(
    string Name,
    string? LogoUrl);
