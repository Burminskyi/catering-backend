using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Shared.Notifications;

public sealed class WorkspaceNotificationService : IWorkspaceNotificationPublisher, IWorkspaceNotificationQueries
{
    private const int DefaultTake = 30;
    private const int MaxTake = 100;

    private readonly AppDbContext _dbContext;

    public WorkspaceNotificationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task PublishAsync(
        WorkspaceNotificationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkspaceId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Type)
            || string.IsNullOrWhiteSpace(request.Title))
        {
            return;
        }

        if (request.Type == WorkspaceNotificationTypes.LowStock
            && request.RelatedEntityId is Guid ingredientId)
        {
            var recentExists = await _dbContext.Set<WorkspaceNotification>()
                .AnyAsync(
                    n => n.WorkspaceId == request.WorkspaceId
                         && n.Type == WorkspaceNotificationTypes.LowStock
                         && n.RelatedEntityId == ingredientId
                         && n.CreatedAtUtc >= DateTime.UtcNow.AddHours(-12),
                    cancellationToken);

            if (recentExists)
            {
                return;
            }
        }

        var audience = string.IsNullOrWhiteSpace(request.Audience)
            ? WorkspaceNotificationAudiences.Operations
            : request.Audience.Trim();

        await _dbContext.Set<WorkspaceNotification>().AddAsync(
            MapNew(request, audience),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertClientMealRequestDigestAsync(
        Guid workspaceId,
        Guid clientCompanyId,
        DateOnly targetDate,
        int submittedCount,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || clientCompanyId == Guid.Empty)
        {
            return;
        }

        var existing = await _dbContext.Set<WorkspaceNotification>()
            .FirstOrDefaultAsync(
                n => n.WorkspaceId == workspaceId
                     && n.Type == WorkspaceNotificationTypes.MealRequestsDigest
                     && n.Audience == WorkspaceNotificationAudiences.ClientAdmin
                     && n.TargetClientCompanyId == clientCompanyId
                     && n.RelatedDate == targetDate,
                cancellationToken);

        if (submittedCount <= 0)
        {
            if (existing is null)
            {
                return;
            }

            _dbContext.Set<WorkspaceNotification>().Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var title = targetDate == today.AddDays(1)
            ? $"{submittedCount} meal request(s) for tomorrow"
            : targetDate == today
                ? $"{submittedCount} meal request(s) for today"
                : $"{submittedCount} meal request(s) for {targetDate:yyyy-MM-dd}";
        var body = "Open consolidation to send the order to catering.";
        var linkPath = "/meal-requests";

        if (existing is null)
        {
            await _dbContext.Set<WorkspaceNotification>().AddAsync(
                new WorkspaceNotification
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    Type = WorkspaceNotificationTypes.MealRequestsDigest,
                    Title = Truncate(title, 200),
                    Body = Truncate(body, 1000),
                    LinkPath = linkPath,
                    Audience = WorkspaceNotificationAudiences.ClientAdmin,
                    TargetClientCompanyId = clientCompanyId,
                    RelatedDate = targetDate,
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);
        }
        else
        {
            var previousTitle = existing.Title;
            existing.Title = Truncate(title, 200);
            existing.Body = Truncate(body, 1000);
            existing.LinkPath = linkPath;
            existing.CreatedAtUtc = DateTime.UtcNow;

            // Count changed → re-alert (clear per-user read marks).
            if (!string.Equals(previousTitle, existing.Title, StringComparison.Ordinal))
            {
                var reads = await _dbContext.Set<WorkspaceNotificationRead>()
                    .Where(r => r.NotificationId == existing.Id)
                    .ToListAsync(cancellationToken);
                if (reads.Count > 0)
                {
                    _dbContext.Set<WorkspaceNotificationRead>().RemoveRange(reads);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceNotificationItem>> ListForUserAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take <= 0 ? DefaultTake : take, 1, MaxTake);

        var candidates = await _dbContext.Set<WorkspaceNotification>()
            .AsNoTracking()
            .Where(n => n.WorkspaceId == workspaceId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take * 4)
            .ToListAsync(cancellationToken);

        var visible = candidates
            .Where(n => WorkspaceNotificationVisibility.IsVisibleToUser(n, viewer))
            .Take(take)
            .ToList();

        if (visible.Count == 0)
        {
            return [];
        }

        var ids = visible.Select(n => n.Id).ToList();
        var readIds = await _dbContext.Set<WorkspaceNotificationRead>()
            .AsNoTracking()
            .Where(r => r.UserId == viewer.UserId && ids.Contains(r.NotificationId))
            .Select(r => r.NotificationId)
            .ToListAsync(cancellationToken);

        var readSet = readIds.ToHashSet();

        return visible
            .Select(n => new WorkspaceNotificationItem(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.LinkPath,
                n.RelatedEntityId,
                n.CreatedAtUtc,
                readSet.Contains(n.Id)))
            .ToList();
    }

    public async Task<int> CountUnreadAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        CancellationToken cancellationToken = default)
    {
        var recent = await _dbContext.Set<WorkspaceNotification>()
            .AsNoTracking()
            .Where(n => n.WorkspaceId == workspaceId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var visibleIds = recent
            .Where(n => WorkspaceNotificationVisibility.IsVisibleToUser(n, viewer))
            .Select(n => n.Id)
            .ToList();

        if (visibleIds.Count == 0)
        {
            return 0;
        }

        var readCount = await _dbContext.Set<WorkspaceNotificationRead>()
            .AsNoTracking()
            .CountAsync(
                r => r.UserId == viewer.UserId && visibleIds.Contains(r.NotificationId),
                cancellationToken);

        return visibleIds.Count - readCount;
    }

    public async Task MarkReadAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Set<WorkspaceNotification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.WorkspaceId == workspaceId, cancellationToken);

        if (notification is null
            || !WorkspaceNotificationVisibility.IsVisibleToUser(notification, viewer))
        {
            return;
        }

        var already = await _dbContext.Set<WorkspaceNotificationRead>()
            .AnyAsync(r => r.NotificationId == notificationId && r.UserId == viewer.UserId, cancellationToken);

        if (already)
        {
            return;
        }

        await _dbContext.Set<WorkspaceNotificationRead>().AddAsync(
            new WorkspaceNotificationRead
            {
                NotificationId = notificationId,
                UserId = viewer.UserId,
                ReadAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(
        Guid workspaceId,
        NotificationViewerContext viewer,
        CancellationToken cancellationToken = default)
    {
        var recent = await _dbContext.Set<WorkspaceNotification>()
            .AsNoTracking()
            .Where(n => n.WorkspaceId == workspaceId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var visibleIds = recent
            .Where(n => WorkspaceNotificationVisibility.IsVisibleToUser(n, viewer))
            .Select(n => n.Id)
            .ToList();

        if (visibleIds.Count == 0)
        {
            return;
        }

        var alreadyRead = await _dbContext.Set<WorkspaceNotificationRead>()
            .AsNoTracking()
            .Where(r => r.UserId == viewer.UserId && visibleIds.Contains(r.NotificationId))
            .Select(r => r.NotificationId)
            .ToListAsync(cancellationToken);

        var unreadIds = visibleIds.Except(alreadyRead).ToList();
        if (unreadIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await _dbContext.Set<WorkspaceNotificationRead>().AddRangeAsync(
            unreadIds.Select(id => new WorkspaceNotificationRead
            {
                NotificationId = id,
                UserId = viewer.UserId,
                ReadAtUtc = now
            }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WorkspaceNotification MapNew(WorkspaceNotificationCreateRequest request, string audience) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Type = request.Type.Trim(),
            Title = Truncate(request.Title.Trim(), 200),
            Body = Truncate(request.Body?.Trim() ?? string.Empty, 1000),
            LinkPath = Truncate(string.IsNullOrWhiteSpace(request.LinkPath) ? "/" : request.LinkPath.Trim(), 300),
            RelatedEntityId = request.RelatedEntityId,
            Audience = Truncate(audience, 32),
            TargetUserId = request.TargetUserId,
            TargetClientCompanyId = request.TargetClientCompanyId,
            RelatedDate = request.RelatedDate,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
