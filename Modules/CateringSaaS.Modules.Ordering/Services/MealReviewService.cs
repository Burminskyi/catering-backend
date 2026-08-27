using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Ordering.Services;

public interface IMealReviewService
{
    Task<MealReviewResponse> CreateAsync(
        CreateMealReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealReviewResponse>> GetForWorkspaceAsync(
        bool? isReclamation,
        CancellationToken cancellationToken = default);
}

public sealed class MealReviewService : IMealReviewService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;

    public MealReviewService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<MealReviewResponse> CreateAsync(
        CreateMealReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureClientEmployee();
        var workspaceId = RequireWorkspace();
        var clientCompanyId = RequireClientCompany();
        var employeeId = RequireUserId();

        if (request.Rating is < 1 or > 5)
        {
            throw new OrderServiceException("Rating must be between 1 and 5.");
        }

        if (request.MenuItemId == Guid.Empty)
        {
            throw new OrderServiceException("MenuItemId is required.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.TargetDate >= today)
        {
            throw new OrderServiceException(
                "Reviews can only be submitted for past delivery dates.");
        }

        var mealRequest = await _dbContext.Set<EmployeeMealRequest>()
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(
                r => r.WorkspaceId == workspaceId
                    && r.ClientCompanyId == clientCompanyId
                    && r.EmployeeId == employeeId
                    && r.TargetDate == request.TargetDate
                    && r.Status == EmployeeMealRequestStatus.Approved,
                cancellationToken);

        if (mealRequest is null)
        {
            throw new OrderServiceException(
                "No approved meal request found for this date.",
                StatusCodes.Status400BadRequest);
        }

        if (mealRequest.Items.All(i => i.MenuItemId != request.MenuItemId))
        {
            throw new OrderServiceException(
                "Menu item was not part of your meal request for this date.",
                StatusCodes.Status400BadRequest);
        }

        var wasDelivered = await _dbContext.Set<Order>()
            .AsNoTracking()
            .AnyAsync(
                o => o.WorkspaceId == workspaceId
                    && o.ClientCompanyId == clientCompanyId
                    && o.TargetDate == request.TargetDate
                    && o.Status == OrderStatus.Delivered,
                cancellationToken);

        if (!wasDelivered)
        {
            throw new OrderServiceException(
                "Cannot review: no delivered catering order for this date.",
                StatusCodes.Status400BadRequest);
        }

        var alreadyReviewed = await _dbContext.Set<MealReview>()
            .AnyAsync(
                r => r.WorkspaceId == workspaceId
                    && r.EmployeeId == employeeId
                    && r.TargetDate == request.TargetDate
                    && r.MenuItemId == request.MenuItemId,
                cancellationToken);

        if (alreadyReviewed)
        {
            throw new OrderServiceException(
                "You already submitted a review for this menu item on this date.",
                StatusCodes.Status409Conflict);
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        if (comment is { Length: > 2000 })
        {
            throw new OrderServiceException("Comment must be at most 2000 characters.");
        }

        var review = new MealReview
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = clientCompanyId,
            EmployeeId = employeeId,
            TargetDate = request.TargetDate,
            MenuItemId = request.MenuItemId,
            Rating = request.Rating,
            Comment = comment,
            PhotoUrl = null,
            IsReclamation = request.Rating <= 2,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Set<MealReview>().AddAsync(review, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(review);
    }

    public async Task<IReadOnlyList<MealReviewResponse>> GetForWorkspaceAsync(
        bool? isReclamation,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var query = _dbContext.Set<MealReview>()
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId);

        if (isReclamation is bool flag)
        {
            query = query.Where(r => r.IsReclamation == flag);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MealReviewResponse(
                r.Id,
                r.WorkspaceId,
                r.ClientCompanyId,
                r.EmployeeId,
                r.TargetDate,
                r.MenuItemId,
                r.Rating,
                r.Comment,
                r.PhotoUrl,
                r.IsReclamation,
                r.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private void EnsureClientEmployee()
    {
        if (!string.Equals(_currentUser.Role, "ClientEmployee", StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderServiceException(
                "Only ClientEmployee can submit meal reviews.",
                StatusCodes.Status403Forbidden);
        }
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private Guid RequireClientCompany()
    {
        if (_currentUser.ClientCompanyId is not Guid clientCompanyId || clientCompanyId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Client company context is required.",
                StatusCodes.Status400BadRequest);
        }

        return clientCompanyId;
    }

    private Guid RequireUserId()
    {
        if (_currentUser.UserId == Guid.Empty)
        {
            throw new OrderServiceException(
                "Authenticated user is required.",
                StatusCodes.Status401Unauthorized);
        }

        return _currentUser.UserId;
    }

    private static MealReviewResponse ToResponse(MealReview review) =>
        new(
            review.Id,
            review.WorkspaceId,
            review.ClientCompanyId,
            review.EmployeeId,
            review.TargetDate,
            review.MenuItemId,
            review.Rating,
            review.Comment,
            review.PhotoUrl,
            review.IsReclamation,
            review.CreatedAt);
}
