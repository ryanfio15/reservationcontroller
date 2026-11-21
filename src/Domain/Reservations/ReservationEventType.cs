namespace Domain.Reservations;

/// <summary>
/// What happened to a reservation. Stored as <c>tinyint</c> in <c>ReservationEvent.EventType</c>.
/// </summary>
/// <remarks>
/// The numbers are part of the schema (B: Logical data model). Renumbering any of them would
/// reinterpret every row already written, so they are fixed.
/// </remarks>
public enum ReservationEventType : byte
{
    /// <summary>The booking was made. The opening event, never a terminal one.</summary>
    Created = 1,

    /// <summary>The diner cancelled. Terminal; returns capacity to the slot.</summary>
    CancelledByDiner = 2,

    /// <summary>The restaurant cancelled, including on a telephone request. Terminal; returns capacity.</summary>
    CancelledByRestaurant = 3,

    /// <summary>The diner was checked in at the host stand. Terminal, with no undo.</summary>
    Redeemed = 4,

    /// <summary>
    /// The diner did not arrive. Terminal. Derived from the clock on every read; written as a
    /// row only by the optional scheduled job, and nothing depends on that happening.
    /// </summary>
    NoShow = 5,
}
