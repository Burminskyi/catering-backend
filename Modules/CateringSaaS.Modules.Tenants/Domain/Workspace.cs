namespace CateringSaaS.Modules.Tenants.Domain;

public class Workspace
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Subdomain { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime SubscriptionExpiresAt { get; set; }

    public required string PlanType { get; set; }
}
