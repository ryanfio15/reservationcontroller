namespace Domain.Reservations;

/// <summary>
/// What the host stand checks a diner in against. Insert-only, one row per reservation.
/// </summary>
/// <remarks>
/// There is no QR column: the QR string is recomputed from the reservation id and the signing key
/// whenever it is shown, and the MAC is never stored (B: Reservation ticket security). The
/// 4-digit code exists only as <see cref="CodeLookupHmac"/>.
/// </remarks>
public sealed class ReservationTicket : ITenantOwned
{
    public Guid ReservationId { get; init; }

    public Guid RestaurantId { get; init; }

    /// <summary>The restaurant's local date. Scopes the 4-digit code, so the same digits can recur on another night.</summary>
    public DateOnly ServiceDate { get; init; }

    /// <summary>HMAC of the 4-digit code under the key named by <see cref="KeyVersion"/>.</summary>
    public byte[] CodeLookupHmac { get; init; } = [];

    /// <summary>
    /// Which attempt produced the code. Four digits inside one restaurant-day will sometimes
    /// collide, and this records how many tries the unique index took.
    /// </summary>
    public byte CodeAttempt { get; init; }

    public string KeyVersion { get; init; } = string.Empty;

    /// <summary>The start of the redemption window. Enforced by the redemption insert, not by a timer.</summary>
    public DateTime RedeemFromUtc { get; init; }

    /// <summary>The end of the redemption window, and the instant a no-show is derived from.</summary>
    public DateTime RedeemUntilUtc { get; init; }
}
