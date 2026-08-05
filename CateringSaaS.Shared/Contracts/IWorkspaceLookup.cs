namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract: Tenants exposes workspace existence checks without leaking the entity.
/// </summary>
public interface IWorkspaceLookup
{
    Task<bool> ExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
