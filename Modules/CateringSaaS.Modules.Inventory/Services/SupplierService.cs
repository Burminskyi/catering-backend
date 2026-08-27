using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Inventory.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<SupplierResponse> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default);

    Task<SupplierResponse> UpdateAsync(
        Guid id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class SupplierService : ISupplierService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public SupplierService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<SupplierResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();

        var query = _dbContext.Set<Supplier>()
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId);

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierResponse> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var name = NormalizeName(request.Name);

        var exists = await _dbContext.Set<Supplier>()
            .AnyAsync(
                s => s.WorkspaceId == workspaceId && s.Name == name && s.IsActive,
                cancellationToken);

        if (exists)
        {
            throw new ServiceException($"Supplier '{name}' already exists.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Phone = NormalizeOptional(request.Phone, 64),
            Email = NormalizeOptional(request.Email, 256),
            Notes = NormalizeOptional(request.Notes, 2000),
            IsActive = true
        };

        await _dbContext.Set<Supplier>().AddAsync(supplier, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> UpdateAsync(
        Guid id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var supplier = await _dbContext.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == workspaceId, cancellationToken);

        if (supplier is null)
        {
            throw new ServiceException($"Supplier '{id}' was not found.", StatusCodes.Status404NotFound);
        }

        var name = NormalizeName(request.Name);

        var duplicate = await _dbContext.Set<Supplier>()
            .AnyAsync(
                s => s.WorkspaceId == workspaceId
                    && s.Name == name
                    && s.IsActive
                    && s.Id != id,
                cancellationToken);

        if (duplicate)
        {
            throw new ServiceException($"Supplier '{name}' already exists.");
        }

        supplier.Name = name;
        supplier.Phone = NormalizeOptional(request.Phone, 64);
        supplier.Email = NormalizeOptional(request.Email, 256);
        supplier.Notes = NormalizeOptional(request.Notes, 2000);
        supplier.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(supplier);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspaceId = RequireWorkspace();
        var supplier = await _dbContext.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == workspaceId, cancellationToken);

        if (supplier is null)
        {
            throw new ServiceException($"Supplier '{id}' was not found.", StatusCodes.Status404NotFound);
        }

        supplier.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid RequireWorkspace()
    {
        if (_tenantContext.WorkspaceId == Guid.Empty)
        {
            throw new ServiceException("Workspace context is required.", StatusCodes.Status400BadRequest);
        }

        return _tenantContext.WorkspaceId;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ServiceException("Supplier name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > 200)
        {
            throw new ServiceException("Supplier name must be at most 200 characters.");
        }

        return normalized;
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
            throw new ServiceException($"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static SupplierResponse ToResponse(Supplier s) =>
        new(s.Id, s.WorkspaceId, s.Name, s.Phone, s.Email, s.Notes, s.IsActive);
}
