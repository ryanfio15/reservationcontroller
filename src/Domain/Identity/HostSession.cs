namespace Domain.Identity;

/// <summary>
/// An unlocked host stand. Opaque: the token is random and carries no claims.
/// </summary>
/// <remarks>
/// Only the SHA-256 of the 256-bit token is stored, so the table cannot be used to impersonate a
/// host. An idle limit and an absolute limit both apply, enforced when a session is validated
/// rather than by a timer.
/// </remarks>
public sealed class HostSession : ITenantOwned
{
    public Guid SessionId { get; init; }

    /// <summary>SHA-256 of the session token.</summary>
    public byte[] TokenHash { get; init; } = [];

    public Guid DeviceId { get; init; }

    public Guid MembershipId { get; init; }

    public Guid RestaurantId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    /// <summary>The absolute limit: the session dies here however active it has been.</summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>Moved forward on use, for the idle limit.</summary>
    public DateTime LastSeenAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }
}
