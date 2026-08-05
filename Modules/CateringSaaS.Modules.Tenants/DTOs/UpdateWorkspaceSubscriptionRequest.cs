namespace CateringSaaS.Modules.Tenants.DTOs;

public sealed record UpdateWorkspaceSubscriptionRequest(
    DateTime SubscriptionExpiresAt,
    string PlanType);
