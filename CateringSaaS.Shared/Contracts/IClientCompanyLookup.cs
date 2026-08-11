namespace CateringSaaS.Shared.Contracts;

public interface IClientCompanyLookup
{
    Task<bool> ExistsInWorkspaceAsync(
        Guid clientCompanyId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
