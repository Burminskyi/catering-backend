namespace CateringSaaS.Shared.Contracts;

public sealed record WorkspaceNotificationItem(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string LinkPath,
    Guid? RelatedEntityId,
    DateTime CreatedAtUtc,
    bool IsRead);

public sealed record NotificationViewerContext(
    Guid UserId,
    string? Role,
    Guid? ClientCompanyId = null,
    string? OrderCadence = null);

public interface IWorkspaceNotificationQueries
{
    Task<IReadOnlyList<WorkspaceNotificationItem>> ListForUserAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        CancellationToken cancellationToken = default);
}
