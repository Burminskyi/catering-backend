namespace CateringSaaS.Modules.Menu.Domain;

public class MenuDay
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid MenuId { get; set; }

    public Menu Menu { get; set; } = null!;

    public DateOnly Date { get; set; }

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
