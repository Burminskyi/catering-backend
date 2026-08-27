namespace CateringSaaS.Modules.Ordering.Domain;

public class MealReview
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid ClientCompanyId { get; set; }

    /// <summary>References Identity User (ClientEmployee) by id — no cross-module EF nav.</summary>
    public Guid EmployeeId { get; set; }

    public DateOnly TargetDate { get; set; }

    /// <summary>References Menu MenuItem by id — no cross-module EF nav.</summary>
    public Guid MenuItemId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
