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
    /// Optional company scope for office-facing users (legacy SaaS feature).
    /// </summary>
    public Guid? CompanyId { get; set; }
}
