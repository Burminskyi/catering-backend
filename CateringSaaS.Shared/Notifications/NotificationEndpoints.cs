using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Shared.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization(policy => policy.RequireRole(
                "WorkspaceAdmin",
                "Manager",
                "SuperAdmin",
                "Chef",
                "Staff",
                "Driver",
                "ClientAdmin"));

        group.MapGet("/", ListAsync)
            .WithName("ListWorkspaceNotifications")
            .WithTags("Notifications");

        group.MapGet("/unread-count", UnreadCountAsync)
            .WithName("WorkspaceNotificationUnreadCount")
            .WithTags("Notifications");

        group.MapPost("/{id:guid}/read", MarkReadAsync)
            .WithName("MarkWorkspaceNotificationRead")
            .WithTags("Notifications");

        group.MapPost("/read-all", MarkAllReadAsync)
            .WithName("MarkAllWorkspaceNotificationsRead")
            .WithTags("Notifications");

        return app;
    }

    private static async Task<IResult> ListAsync(
        IWorkspaceNotificationQueries queries,
        IClientCompanyLookup clientCompanies,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        int? take,
        CancellationToken cancellationToken)
    {
        var viewer = await BuildViewerAsync(
            clientCompanies,
            tenantContext,
            currentUser,
            cancellationToken);

        if (viewer is null)
        {
            return Results.Unauthorized();
        }

        var items = await queries.ListForUserAsync(
            tenantContext.WorkspaceId,
            viewer,
            take ?? 30,
            cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> UnreadCountAsync(
        IWorkspaceNotificationQueries queries,
        IClientCompanyLookup clientCompanies,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var viewer = await BuildViewerAsync(
            clientCompanies,
            tenantContext,
            currentUser,
            cancellationToken);

        if (viewer is null)
        {
            return Results.Unauthorized();
        }

        var count = await queries.CountUnreadAsync(
            tenantContext.WorkspaceId,
            viewer,
            cancellationToken);

        return Results.Ok(new { count });
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id,
        IWorkspaceNotificationQueries queries,
        IClientCompanyLookup clientCompanies,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var viewer = await BuildViewerAsync(
            clientCompanies,
            tenantContext,
            currentUser,
            cancellationToken);

        if (viewer is null)
        {
            return Results.Unauthorized();
        }

        await queries.MarkReadAsync(
            tenantContext.WorkspaceId,
            viewer,
            id,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(
        IWorkspaceNotificationQueries queries,
        IClientCompanyLookup clientCompanies,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var viewer = await BuildViewerAsync(
            clientCompanies,
            tenantContext,
            currentUser,
            cancellationToken);

        if (viewer is null)
        {
            return Results.Unauthorized();
        }

        await queries.MarkAllReadAsync(
            tenantContext.WorkspaceId,
            viewer,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<NotificationViewerContext?> BuildViewerAsync(
        IClientCompanyLookup clientCompanies,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (tenantContext.WorkspaceId == Guid.Empty || currentUser.UserId == Guid.Empty)
        {
            return null;
        }

        string? orderCadence = null;
        if (string.Equals(currentUser.Role, "ClientAdmin", StringComparison.OrdinalIgnoreCase)
            && currentUser.ClientCompanyId is Guid companyId
            && companyId != Guid.Empty)
        {
            orderCadence = await clientCompanies.GetOrderCadenceAsync(
                tenantContext.WorkspaceId,
                companyId,
                cancellationToken);
        }

        return new NotificationViewerContext(
            currentUser.UserId,
            currentUser.Role,
            currentUser.ClientCompanyId,
            orderCadence);
    }
}
