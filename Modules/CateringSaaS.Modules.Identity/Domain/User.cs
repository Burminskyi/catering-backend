namespace CateringSaaS.Modules.Identity.Domain;

public class User
{
    public Guid Id { get; set; }

    public Guid? WorkspaceId { get; set; }

    public required string Username { get; set; }

    public string? Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? AvatarUrl { get; set; }

    public StaffRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional B2B2B client company scope (ClientCompany entity lives in Tenants module).
    /// </summary>
    public Guid? ClientCompanyId { get; set; }

    /// <summary>
    /// Legacy company scope for office-facing users.
    /// </summary>
    public Guid? CompanyId { get; set; }
}
