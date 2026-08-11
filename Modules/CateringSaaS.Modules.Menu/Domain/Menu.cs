namespace CateringSaaS.Modules.Menu.Domain;

public class Menu
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    /// <summary>Null = general template; set = client-specific menu.</summary>
    public Guid? ClientCompanyId { get; set; }

    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public MenuStatus Status { get; set; } = MenuStatus.Draft;

    public ICollection<MenuDay> Days { get; set; } = new List<MenuDay>();
}
