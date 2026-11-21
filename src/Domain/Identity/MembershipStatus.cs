namespace Domain.Identity;

/// <summary>
/// Whether a staff membership is usable. Stored as <c>tinyint</c>.
/// </summary>
public enum MembershipStatus : byte
{
    /// <summary>Created, but the invited person has not signed in yet.</summary>
    Invited = 1,

    Active = 2,

    /// <summary>
    /// Deactivated. Kept rather than deleted, because insert-only reservation and audit rows
    /// reference the membership that acted.
    /// </summary>
    Deactivated = 3,
}
