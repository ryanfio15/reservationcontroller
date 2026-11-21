namespace Domain.Reservations;

/// <summary>
/// A booking. Insert-only: it has no status column, and its life is told by
/// <c>ReservationEvent</c> rows.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="DinerUserId"/> and <see cref="CreatedByMembershipId"/> is set — a
/// diner booked it, or a host added a walk-in or telephone booking — and a CHECK constraint
/// enforces that.
/// </remarks>
public sealed class Reservation : ITenantOwned
{
    public Guid ReservationId { get; init; }

    public Guid RestaurantId { get; init; }

    /// <summary>The diner who booked. Null for a staff-created booking.</summary>
    public Guid? DinerUserId { get; init; }

    /// <summary>The staff member who created it. Null for a diner booking.</summary>
    public Guid? CreatedByMembershipId { get; init; }

    public long SlotId { get; init; }

    public DateTime SlotStartUtc { get; init; }

    public DateOnly LocalDate { get; init; }

    public TimeOnly LocalTime { get; init; }

    public int PartySize { get; init; }

    /// <summary>
    /// Extension point for rebooking, which is a Production item: a new reservation would point
    /// at the one it replaces. Nothing writes it in the prototype.
    /// </summary>
    public Guid? SupersedesReservationId { get; init; }

    /// <summary>
    /// The caller's idempotency key. Unique per creator, so a retried request returns the
    /// original reservation instead of making a second one.
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// SHA-256 of the request body. A replay with the same key but a different body is a
    /// mistake, not a retry, and is answered with 422.
    /// </summary>
    public byte[] RequestHash { get; init; } = [];

    public DateTime CreatedAtUtc { get; init; }
}
