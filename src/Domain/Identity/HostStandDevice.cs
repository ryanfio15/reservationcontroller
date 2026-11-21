namespace Domain.Identity;

/// <summary>
/// A tablet at a host stand.
/// </summary>
/// <remarks>
/// <para>
/// A device must be paired by a manager, with a one-time code generated on the manager's own
/// device, before any 4-digit host code works on it (A2, fixed: the code alone is too weak).
/// Pairing sets <see cref="CredentialHash"/> and clears the pairing columns.
/// </para>
/// <para>
/// Nothing here is a secret in plaintext: the credential and the pairing code are stored as
/// SHA-256 hashes (B: Security of stored data). The row is locked with <c>UPDLOCK</c> for each
/// sign-in attempt, which is what makes the lockout count correct under concurrency instead of
/// depending on a rate limiter that vanishes at scale to zero.
/// </para>
/// </remarks>
public sealed class HostStandDevice : ITenantOwned
{
    public Guid DeviceId { get; init; }

    public Guid RestaurantId { get; init; }

    public string Label { get; set; } = string.Empty;

    /// <summary>SHA-256 of the manager's one-time pairing code. Null once paired.</summary>
    public byte[]? PairingCodeHash { get; set; }

    /// <summary>When the pairing code dies: ten minutes, or one use.</summary>
    public DateTime? PairingExpiresAtUtc { get; set; }

    /// <summary>SHA-256 of the device's long-lived credential. Null until paired.</summary>
    public byte[]? CredentialHash { get; set; }

    /// <summary>The manager who created the device, for the audit trail.</summary>
    public Guid CreatedByMembershipId { get; init; }

    public DateTime? PairedAtUtc { get; set; }

    /// <summary>Set by the lockout rule after too many failed sign-ins inside the window.</summary>
    public DateTime? LockedUntilUtc { get; set; }

    /// <summary>Set on crossing the suspension threshold. Cleared only by a manager reinstating the device.</summary>
    public DateTime? SuspendedAtUtc { get; set; }

    /// <summary>
    /// Anchors the lockout windows after a reinstatement, so the attempts that caused the
    /// suspension are not counted again and the device is not immediately re-locked.
    /// </summary>
    public DateTime? LockoutResetAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
}
