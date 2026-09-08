namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module contract: resolve user display names without leaking Identity entities.
/// </summary>
public interface IUserDisplayLookup
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}
