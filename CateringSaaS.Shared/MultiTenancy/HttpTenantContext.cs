using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Shared.MultiTenancy;

/// <summary>
/// Resolves the current workspace from the authenticated user's JWT <c>workspaceId</c> claim.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid WorkspaceId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Guid.Empty;
            }

            var raw = user.FindFirstValue("workspaceId");
            return Guid.TryParse(raw, out var workspaceId) ? workspaceId : Guid.Empty;
        }
    }
}
