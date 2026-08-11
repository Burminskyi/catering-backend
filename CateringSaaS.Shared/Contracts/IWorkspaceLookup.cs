namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract: resolve workspace context without leaking Tenants entities.
/// </summary>
public interface IWorkspaceLookup
{
    Task<bool> ExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<Guid?> ResolveWorkspaceIdBySubdomainAsync(
        string subdomain,
        CancellationToken cancellationToken = default);
}
