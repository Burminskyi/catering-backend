using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Services;

public interface IWorkspaceBrandingService
{
    Task<WorkspaceBrandingResponse> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<WorkspaceBrandingResponse> UpdateCurrentAsync(
        UpdateWorkspaceBrandingRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class WorkspaceBrandingService : IWorkspaceBrandingService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public WorkspaceBrandingService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<WorkspaceBrandingResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var workspace = await GetCurrentWorkspaceAsync(cancellationToken);
        return ToResponse(workspace);
    }

    public async Task<WorkspaceBrandingResponse> UpdateCurrentAsync(
        UpdateWorkspaceBrandingRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new WorkspaceBrandingException("Company name is required.", StatusCodes.Status400BadRequest);
        }

        if (name.Length > 200)
        {
            throw new WorkspaceBrandingException("Company name must be 200 characters or fewer.", StatusCodes.Status400BadRequest);
        }

        var logoUrl = NormalizeLogoUrl(request.LogoUrl);
        var workspace = await GetCurrentWorkspaceAsync(cancellationToken);

        workspace.Name = name;
        workspace.LogoUrl = logoUrl;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(workspace);
    }

    private async Task<Workspace> GetCurrentWorkspaceAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new WorkspaceBrandingException("Workspace context is required.", StatusCodes.Status400BadRequest);
        }

        var workspace = await _dbContext.Set<Workspace>()
            .FirstOrDefaultAsync(w => w.Id == _tenantContext.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            throw new WorkspaceBrandingException("Workspace was not found.", StatusCodes.Status404NotFound);
        }

        return workspace;
    }

    private static string? NormalizeLogoUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 2048)
        {
            throw new WorkspaceBrandingException("Logo URL must be 2048 characters or fewer.", StatusCodes.Status400BadRequest);
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new WorkspaceBrandingException("Logo URL must be an absolute http or https URL.", StatusCodes.Status400BadRequest);
        }

        return trimmed;
    }

    private static WorkspaceBrandingResponse ToResponse(Workspace workspace) =>
        new(workspace.Id, workspace.Name, workspace.LogoUrl, workspace.Subdomain);
}

public sealed class WorkspaceBrandingException : Exception
{
    public int StatusCode { get; }

    public WorkspaceBrandingException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
