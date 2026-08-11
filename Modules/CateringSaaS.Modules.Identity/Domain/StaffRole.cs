namespace CateringSaaS.Modules.Identity.Domain;

/// <summary>
/// Platform-level SuperAdmin plus workspace staff roles.
/// </summary>
public enum StaffRole
{
    SuperAdmin = 0,
    WorkspaceAdmin = 1,
    Manager = 2,
    Chef = 3,
    Driver = 4,
    Staff = 5
}
