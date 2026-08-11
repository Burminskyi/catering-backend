namespace CateringSaaS.Modules.Identity.Domain;

/// <summary>
/// Platform-level SuperAdmin, workspace staff roles, and B2B2B client roles.
/// </summary>
public enum StaffRole
{
    SuperAdmin = 0,
    WorkspaceAdmin = 1,
    Manager = 2,
    Chef = 3,
    Driver = 4,
    Staff = 5,
    ClientAdmin = 6,
    ClientEmployee = 7
}
