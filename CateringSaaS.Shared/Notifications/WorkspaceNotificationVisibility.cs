using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Shared.Notifications;

internal static class WorkspaceNotificationVisibility
{
    public static bool IsVisibleToUser(
        WorkspaceNotification notification,
        NotificationViewerContext viewer)
    {
        var audience = string.IsNullOrWhiteSpace(notification.Audience)
            ? WorkspaceNotificationAudiences.Operations
            : notification.Audience.Trim();

        if (IsOperationsRole(viewer.Role))
        {
            return string.Equals(audience, WorkspaceNotificationAudiences.Operations, StringComparison.Ordinal);
        }

        if (IsKitchenRole(viewer.Role))
        {
            return string.Equals(audience, WorkspaceNotificationAudiences.Kitchen, StringComparison.Ordinal);
        }

        if (string.Equals(viewer.Role, "Driver", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(audience, WorkspaceNotificationAudiences.Driver, StringComparison.Ordinal)
                   && notification.TargetUserId == viewer.UserId;
        }

        if (string.Equals(viewer.Role, "ClientAdmin", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(audience, WorkspaceNotificationAudiences.ClientAdmin, StringComparison.Ordinal))
            {
                return false;
            }

            if (viewer.ClientCompanyId is not Guid companyId
                || companyId == Guid.Empty
                || notification.TargetClientCompanyId != companyId)
            {
                return false;
            }

            return IsRelatedDateInCadenceWindow(notification.RelatedDate, viewer.OrderCadence);
        }

        return false;
    }

    /// <summary>
    /// Daily: today + tomorrow. Weekly: today through today+6.
    /// Digests outside the window stay stored but hidden until they enter the window.
    /// </summary>
    public static bool IsRelatedDateInCadenceWindow(DateOnly? relatedDate, string? orderCadence)
    {
        if (relatedDate is null)
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizonDays = string.Equals(orderCadence, "Weekly", StringComparison.OrdinalIgnoreCase)
            ? 6
            : 1;

        var end = today.AddDays(horizonDays);
        return relatedDate.Value >= today && relatedDate.Value <= end;
    }

    private static bool IsOperationsRole(string? role) =>
        string.Equals(role, "WorkspaceAdmin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool IsKitchenRole(string? role) =>
        string.Equals(role, "Chef", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);
}
