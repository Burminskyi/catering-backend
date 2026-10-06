namespace CateringSaaS.Shared.Contracts;

public sealed record ClientCompanyContact(
    Guid Id,
    string Name,
    string? Address,
    string? ContactPhone,
    string? ContactName);

public interface IClientCompanyLookup
{
    Task<bool> ExistsInWorkspaceAsync(
        Guid clientCompanyId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ClientCompanyContact>> GetContactsAsync(
        Guid workspaceId,
        IEnumerable<Guid> clientCompanyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Case-insensitive partial name search within a workspace. WorkspaceId must come from JWT scope.
    /// </summary>
    Task<IReadOnlyList<ClientCompanyContact>> SearchByNameAsync(
        Guid workspaceId,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
