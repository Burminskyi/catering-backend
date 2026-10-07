namespace CateringSaaS.Shared.Notifications;

public static class WorkspaceNotificationTypes
{
    public const string LowStock = "low_stock";
    public const string NewReclamation = "new_reclamation";
    public const string OrderCreated = "order_created";
    public const string OrderCancelled = "order_cancelled";
    public const string OrderInProduction = "order_in_production";
    public const string OrderReadyForDelivery = "order_ready_for_delivery";
    public const string DeliveryAssigned = "delivery_assigned";
    public const string MealRequestsDigest = "meal_requests_digest";
}

public static class WorkspaceNotificationAudiences
{
    public const string Operations = "operations";
    public const string Kitchen = "kitchen";
    public const string Driver = "driver";
    public const string ClientAdmin = "client_admin";
}

/// <summary>
/// Workspace-scoped inbox item; visibility is filtered by <see cref="Audience"/> and optional <see cref="TargetUserId"/>.
/// Read state is tracked per user in <see cref="WorkspaceNotificationRead"/>.
/// </summary>
public sealed class WorkspaceNotification
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>In-app path, e.g. /inventory or /orders.</summary>
    public string LinkPath { get; set; } = string.Empty;

    public Guid? RelatedEntityId { get; set; }

    /// <summary>operations | kitchen | driver</summary>
    public string Audience { get; set; } = WorkspaceNotificationAudiences.Operations;

    /// <summary>When set with driver audience, only this user sees the notification.</summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>When set with client_admin audience, only admins of this company see it.</summary>
    public Guid? TargetClientCompanyId { get; set; }

    /// <summary>Delivery / meal-request date for digests (cadence window filter).</summary>
    public DateOnly? RelatedDate { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WorkspaceNotificationRead
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime ReadAtUtc { get; set; }

    public WorkspaceNotification? Notification { get; set; }
}
