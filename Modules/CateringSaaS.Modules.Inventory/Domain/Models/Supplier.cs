namespace CateringSaaS.Modules.Inventory.Domain.Models;

public class Supplier
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public required string Name { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
