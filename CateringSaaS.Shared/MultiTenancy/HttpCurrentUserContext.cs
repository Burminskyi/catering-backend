using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Shared.MultiTenancy;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Guid.Empty;
            }

            var raw = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var userId) ? userId : Guid.Empty;
        }
    }

    public Guid? ClientCompanyId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var raw = user.FindFirstValue("clientCompanyId");
            return Guid.TryParse(raw, out var clientCompanyId) ? clientCompanyId : null;
        }
    }

    public string? Role =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");
}
