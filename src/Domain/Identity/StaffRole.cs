namespace Domain.Identity;

/// <summary>
/// What a staff member may do. Stored as <c>tinyint</c> in <c>StaffMembership.Role</c>.
/// </summary>
/// <remarks>
/// The numbers are part of the schema (B: Logical data model) and are fixed.
///
/// Owners and managers have identical privileges, with one exception that is a fixed decision
/// (A2): a manager cannot remove or demote an owner or grant the owner role, and a restaurant can
/// never be left without an active owner.
/// </remarks>
public enum StaffRole : byte
{
    Owner = 1,
    Manager = 2,

    /// <summary>Works the host stand. May hold no user account at all — a code-only host.</summary>
    Host = 3,
}
