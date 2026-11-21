namespace Domain.Reservations;

/// <summary>
/// Something that happened to a reservation. Insert-only, ordered, and the only record of state.
/// </summary>
/// <remarks>
/// <para>
/// A filtered unique index on <c>(ReservationId) WHERE IsTerminal = 1</c> makes "at most one
/// terminal event" a database guarantee: it is what stops a reservation being both redeemed and
/// cancelled, under any amount of concurrency, without a lock. A CHECK constraint ties
/// <see cref="IsTerminal"/> to <see cref="EventType"/> so the index cannot be dodged by lying
/// about the flag.
/// </para>
/// <para>
/// The ordered <c>bigint</c> key is also the notification extension point: a dispatcher would
/// read this log by <see cref="EventId"/> cursor.
/// </para>
/// </remarks>
public sealed class ReservationEvent : ITenantOwned
{
    public long EventId { get; init; }

    public Guid ReservationId { get; init; }

    public Guid RestaurantId { get; init; }

    public ReservationEventType EventType { get; init; }

    /// <summary>
    /// Must equal <see cref="ReservationLifecycle.IsTerminal"/> for <see cref="EventType"/>.
    /// Stored rather than derived because a filtered index needs a column to filter on.
    /// </summary>
    public bool IsTerminal { get; init; }

    /// <summary>The diner who acted, if it was a diner.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>The staff member who acted, if it was staff.</summary>
    public Guid? ActorMembershipId { get; init; }

    /// <summary>The host stand that acted, for a redemption.</summary>
    public Guid? DeviceId { get; init; }

    /// <summary>Always the caller's <c>@NowUtc</c>, never a database clock (B: Time zones).</summary>
    public DateTime OccurredAtUtc { get; init; }
}
