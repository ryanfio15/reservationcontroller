namespace Domain.Auditing;

/// <summary>
/// A security-relevant action: host sign-ins, codes issued or revoked, role changes, device
/// pairing and suspension. Insert-only.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DetailJson"/> holds ids and enum values only (B: Cross-cutting concerns). No
/// 4-digit code, token, QR string or session identifier ever goes in it: an audit trail that
/// records credentials is a credential store.
/// </para>
/// <para>
/// This is the one tenant-scoped table whose <c>RestaurantId</c> is nullable, because some
/// actions — onboarding a restaurant, erasing an account — happen outside any one tenant. It
/// therefore does not implement <see cref="ITenantOwned"/>, and its query filter admits the
/// null rows deliberately rather than by omission.
/// </para>
/// </remarks>
public sealed class AuditLog
{
    public long AuditId { get; init; }

    /// <summary>Null for an action that belongs to no single restaurant.</summary>
    public Guid? RestaurantId { get; init; }

    public Guid? ActorUserId { get; init; }

    public Guid? ActorMembershipId { get; init; }

    public Guid? DeviceId { get; init; }

    /// <summary>What was done, as a stable short name such as <c>HostSignInSucceeded</c>.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>What it was done to, such as <c>HostStandDevice</c>.</summary>
    public string TargetType { get; init; } = string.Empty;

    /// <summary>
    /// The target's identifier as text, because targets are keyed variously by
    /// <c>uniqueidentifier</c> and <c>bigint</c>.
    /// </summary>
    public string TargetId { get; init; } = string.Empty;

    /// <summary>Ids and enum values only. Never a credential.</summary>
    public string? DetailJson { get; init; }

    public DateTime OccurredAtUtc { get; init; }
}
