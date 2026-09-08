using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public interface IMealRequestDeliverySync
{
    /// <summary>
    /// Marks Approved meal requests for the order's company/date as Delivered.
    /// Idempotent for already-Delivered rows.
    /// </summary>
    Task SyncDeliveredAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class MealRequestDeliverySync : IMealRequestDeliverySync
{
    private readonly AppDbContext _dbContext;

    public MealRequestDeliverySync(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SyncDeliveredAsync(Order order, CancellationToken cancellationToken = default)
    {
        var requests = await _dbContext.Set<EmployeeMealRequest>()
            .Where(r => r.WorkspaceId == order.WorkspaceId
                && r.ClientCompanyId == order.ClientCompanyId
                && r.TargetDate == order.TargetDate
                && r.Status == EmployeeMealRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            return;
        }

        foreach (var request in requests)
        {
            request.Status = EmployeeMealRequestStatus.Delivered;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
