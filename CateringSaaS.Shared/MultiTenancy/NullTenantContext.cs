namespace CateringSaaS.Shared.MultiTenancy;

/// <summary>
/// Fallback tenant context used when no HTTP workspace header/claim is available
/// (e.g. login, seeding). Returns <see cref="Guid.Empty"/>.
/// </summary>
public sealed class NullTenantContext : ITenantContext
{
    public Guid WorkspaceId => Guid.Empty;
}
