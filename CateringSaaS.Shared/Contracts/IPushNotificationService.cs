namespace CateringSaaS.Shared.Contracts;

/// <summary>
/// Cross-module push/notification hook. Real providers can replace the logging stub.
/// </summary>
public interface IPushNotificationService
{
    Task NotifyEmployeesOrderDeliveredAsync(
        Guid clientCompanyId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
