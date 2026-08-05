namespace CateringSaaS.Modules.Tenants.DTOs;

public sealed record WorkspaceResponse(
    Guid Id,
    string Name,
    string Subdomain,
    DateTime CreatedAt,
    bool IsActive,
    DateTime SubscriptionExpiresAt,
    string PlanType);
