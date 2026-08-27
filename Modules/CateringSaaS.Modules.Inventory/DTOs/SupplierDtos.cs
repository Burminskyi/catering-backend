namespace CateringSaaS.Modules.Inventory.DTOs;

public sealed record CreateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Notes);

public sealed record UpdateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsActive);

public sealed record SupplierResponse(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsActive);
