namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract: Identity provisions and updates CateringManager accounts for workspaces.
/// </summary>
public interface IWorkspaceManagerProvisioner
{
    Task ProvisionCateringManagerAsync(
        Guid workspaceId,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task UpdatePrimaryCateringManagerAsync(
        Guid workspaceId,
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
