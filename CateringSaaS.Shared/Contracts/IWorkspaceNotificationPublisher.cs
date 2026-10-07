namespace CateringSaaS.Shared.Contracts;

public sealed record WorkspaceNotificationCreateRequest(
    Guid WorkspaceId,
    string Type,
    string Title,
    string Body,
    string LinkPath,
    Guid? RelatedEntityId = null,
    string Audience = "operations",
    Guid? TargetUserId = null,
    Guid? TargetClientCompanyId = null,
    DateOnly? RelatedDate = null);

public interface IWorkspaceNotificationPublisher
{
    Task PublishAsync(
        WorkspaceNotificationCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts one client-admin digest per company + target date. Removes it when submittedCount is 0.
    /// </summary>
    Task UpsertClientMealRequestDigestAsync(
        Guid workspaceId,
        Guid clientCompanyId,
        DateOnly targetDate,
        int submittedCount,
        CancellationToken cancellationToken = default);
}
