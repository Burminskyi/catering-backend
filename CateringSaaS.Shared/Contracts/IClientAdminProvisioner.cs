namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract: Identity provisions ClientAdmin users for a ClientCompany.
/// </summary>
public interface IClientAdminProvisioner
{
    Task ProvisionClientAdminAsync(
        Guid workspaceId,
        Guid clientCompanyId,
        string username,
        string password,
        string? email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);
}
