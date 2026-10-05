namespace CateringSaaS.Modules.Tenants.Domain;

public class ClientCompany
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;

    public required string Name { get; set; }

    public string? Address { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactName { get; set; }

    public bool IsActive { get; set; } = true;
}
