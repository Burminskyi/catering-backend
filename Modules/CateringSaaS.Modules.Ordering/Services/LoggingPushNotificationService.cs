using CateringSaaS.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Modules.Ordering.Services;

/// <summary>Development stub — logs instead of sending real push notifications.</summary>
public sealed class LoggingPushNotificationService : IPushNotificationService
{
    private readonly ILogger<LoggingPushNotificationService> _logger;

    public LoggingPushNotificationService(ILogger<LoggingPushNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyEmployeesOrderDeliveredAsync(
        Guid clientCompanyId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Push (stub): order delivered for ClientCompanyId={ClientCompanyId}, TargetDate={TargetDate}",
            clientCompanyId,
            targetDate);

        return Task.CompletedTask;
    }
}
