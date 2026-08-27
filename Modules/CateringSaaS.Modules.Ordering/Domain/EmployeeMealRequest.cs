namespace CateringSaaS.Modules.Ordering.Domain;

public class EmployeeMealRequest
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid ClientCompanyId { get; set; }

    /// <summary>References Identity User (ClientEmployee) by id — no cross-module EF nav.</summary>
    public Guid EmployeeId { get; set; }

    public DateOnly TargetDate { get; set; }

    public EmployeeMealRequestStatus Status { get; set; } = EmployeeMealRequestStatus.Draft;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<EmployeeMealRequestItem> Items { get; set; } = new List<EmployeeMealRequestItem>();
}
