namespace CateringSaaS.Modules.Identity.Domain;

public class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public AppRole Role { get; set; }

    public Guid? WorkspaceId { get; set; }

    public Guid? CompanyId { get; set; }
}
