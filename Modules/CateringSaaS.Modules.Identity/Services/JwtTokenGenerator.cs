using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CateringSaaS.Modules.Identity.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CateringSaaS.Modules.Identity.Services;

public sealed class JwtTokenGenerator
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string? _audience;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = configuration["Jwt:Audience"];
    }

    public string Generate(User user) =>
        Generate(
            userId: user.Id,
            role: user.Role.ToString(),
            workspaceId: user.WorkspaceId,
            clientCompanyId: user.ClientCompanyId,
            companyId: user.CompanyId);

    public string GenerateImpersonationToken(User manager, Guid impersonatedByUserId) =>
        Generate(
            userId: manager.Id,
            role: StaffRole.WorkspaceAdmin.ToString(),
            workspaceId: manager.WorkspaceId,
            clientCompanyId: manager.ClientCompanyId,
            companyId: manager.CompanyId,
            extraClaims:
            [
                new Claim("impersonation", "true"),
                new Claim("impersonatedBy", impersonatedByUserId.ToString())
            ]);

    private string Generate(
        Guid userId,
        string role,
        Guid? workspaceId,
        Guid? clientCompanyId,
        Guid? companyId,
        IEnumerable<Claim>? extraClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("role", role)
        };

        if (workspaceId is Guid wsId)
        {
            claims.Add(new Claim("workspaceId", wsId.ToString()));
        }

        if (clientCompanyId is Guid ccId)
        {
            claims.Add(new Claim("clientCompanyId", ccId.ToString()));
        }

        if (companyId is Guid cId)
        {
            claims.Add(new Claim("companyId", cId.ToString()));
        }

        if (extraClaims is not null)
        {
            claims.AddRange(extraClaims);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
