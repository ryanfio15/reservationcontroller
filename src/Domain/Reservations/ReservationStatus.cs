namespace Domain.Reservations;

/// <summary>
/// A reservation's status, as <see cref="ReservationLifecycle.Status"/> derives it.
/// </summary>
/// <remarks>
/// Never stored and never computed by SQL (B: Logical data model), so these numbers are free to
/// change — unlike <see cref="ReservationEventType"/>, which is persisted. The two cancellations
/// stay distinct because the screens say who cancelled, and a diner reading "cancelled by the
/// restaurant" needs to know it was not them.
/// </remarks>
public enum ReservationStatus
{
    /// <summary>Booked, not yet redeemed, and its redemption window has not passed.</summary>
    Confirmed,

    /// <summary>The diner cancelled.</summary>
    CancelledByDiner,

    /// <summary>The restaurant cancelled, including on a telephone request.</summary>
    CancelledByRestaurant,

    /// <summary>Checked in at the host stand.</summary>
    Redeemed,

    /// <summary>The redemption window passed with no check-in.</summary>
    NoShow,
}
