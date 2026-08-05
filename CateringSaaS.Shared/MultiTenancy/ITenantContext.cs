namespace CateringSaaS.Shared.MultiTenancy;

public interface ITenantContext
{
    Guid WorkspaceId { get; }
}
