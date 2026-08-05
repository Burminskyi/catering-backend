namespace CateringSaaS.Modules.Tenants.DTOs;

public sealed record CreateWorkspaceRequest(
    string Name,
    string Subdomain,
    DateTime SubscriptionExpiresAt,
    string PlanType,
    string ManagerEmail,
    string ManagerPassword);
