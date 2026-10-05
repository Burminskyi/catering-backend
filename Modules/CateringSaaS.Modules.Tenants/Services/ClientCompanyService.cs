using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Modules.Tenants.DTOs;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Tenants.Services;

public interface IClientCompanyService
{
    Task<IReadOnlyList<ClientCompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ClientCompanyResponse> CreateAsync(
        CreateClientCompanyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ClientCompanyService : IClientCompanyService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IClientAdminProvisioner _clientAdminProvisioner;

    public ClientCompanyService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        IClientAdminProvisioner clientAdminProvisioner)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _clientAdminProvisioner = clientAdminProvisioner;
    }

    public async Task<IReadOnlyList<ClientCompanyResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        return await _dbContext.Set<ClientCompany>()
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.Name)
            .Select(c => new ClientCompanyResponse(
                c.Id,
                c.WorkspaceId,
                c.Name,
                c.IsActive,
                c.Address,
                c.ContactPhone,
                c.ContactName))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientCompanyResponse> CreateAsync(
        CreateClientCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new TenantServiceException("Name is required.");
        }

        var hasAdminDetails =
            !string.IsNullOrWhiteSpace(request.AdminUsername)
            || !string.IsNullOrWhiteSpace(request.AdminPassword)
            || !string.IsNullOrWhiteSpace(request.AdminEmail)
            || !string.IsNullOrWhiteSpace(request.AdminFirstName)
            || !string.IsNullOrWhiteSpace(request.AdminLastName);

        if (hasAdminDetails)
        {
            if (string.IsNullOrWhiteSpace(request.AdminUsername)
                || string.IsNullOrWhiteSpace(request.AdminPassword)
                || string.IsNullOrWhiteSpace(request.AdminFirstName)
                || string.IsNullOrWhiteSpace(request.AdminLastName))
            {
                throw new TenantServiceException(
                    "AdminUsername, AdminPassword, AdminFirstName, and AdminLastName are required when provisioning a ClientAdmin.");
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var client = new ClientCompany
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Name = request.Name.Trim(),
                Address = NormalizeOptional(request.Address, 500),
                ContactPhone = NormalizeOptional(request.ContactPhone, 40),
                ContactName = NormalizeOptional(request.ContactName, 200),
                IsActive = true
            };

            await _dbContext.Set<ClientCompany>().AddAsync(client, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (hasAdminDetails)
            {
                await _clientAdminProvisioner.ProvisionClientAdminAsync(
                    workspaceId,
                    client.Id,
                    request.AdminUsername!,
                    request.AdminPassword!,
                    request.AdminEmail,
                    request.AdminFirstName!,
                    request.AdminLastName!,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new ClientCompanyResponse(
                client.Id,
                client.WorkspaceId,
                client.Name,
                client.IsActive,
                client.Address,
                client.ContactPhone,
                client.ContactName);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new TenantServiceException($"Value exceeds {maxLength} characters.");
        }

        return trimmed;
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new TenantServiceException(
                "Workspace context is required.",
                StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }
}

public sealed class TenantServiceException : Exception
{
    public int StatusCode { get; }

    public TenantServiceException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
