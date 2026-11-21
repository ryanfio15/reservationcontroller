namespace Domain.Restaurants;

/// <summary>
/// One bookable slot for one party band: the row the booking transaction contends on.
/// </summary>
/// <remarks>
/// <para>
/// <c>CHECK (Booked BETWEEN 0 AND Capacity)</c> is the last line of defence against overselling:
/// even if every layer above were wrong, the database refuses the row. It is also why capacity is
/// shrunk with a set-based <c>CASE WHEN @New &gt; Booked</c> statement rather than read-then-write
/// — the latter would race a booking and trip the CHECK (B: Core domain logic).
/// </para>
/// <para>
/// Both <see cref="SlotStartUtc"/> and the local date and time are stored, so availability
/// queries never do time-zone arithmetic in SQL (B: Search and availability queries).
/// </para>
/// </remarks>
public sealed class SlotInventory : ITenantOwned
{
    public long SlotId { get; init; }

    public Guid RestaurantId { get; init; }

    /// <summary>The rule that produced this slot. Null once that rule is gone; the slot stays, because reservations reference it.</summary>
    public Guid? RuleId { get; set; }

    public DateTime SlotStartUtc { get; init; }

    /// <summary>The restaurant's local date — the "service date".</summary>
    public DateOnly LocalDate { get; init; }

    public TimeOnly LocalTime { get; init; }

    public int PartyMin { get; init; }

    public int PartyMax { get; set; }

    public int Capacity { get; set; }

    public int Booked { get; set; }

    /// <summary>
    /// Closed slots are never deleted, because <c>Reservation.SlotId</c> references them.
    /// Reservations already in a closed slot stay valid and redeemable; the admin list flags them
    /// "outside current hours".
    /// </summary>
    public bool IsOpen { get; set; }
}
