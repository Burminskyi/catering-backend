namespace CateringSaaS.Modules.Identity.DTOs;

public sealed record ImpersonationResponse(
    string AccessToken,
    Guid WorkspaceId,
    Guid UserId,
    string Role);
