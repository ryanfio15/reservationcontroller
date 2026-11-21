namespace Domain.Reservations;

/// <summary>
/// How a host found the reservation they tried to redeem. Stored as <c>tinyint</c> in
/// <c>RedemptionAttempt.Method</c>.
/// </summary>
public enum RedemptionMethod : byte
{
    /// <summary>Scanned the QR code.</summary>
    Qr = 1,

    /// <summary>Typed the diner's 4-digit code. This is the path the throttle protects.</summary>
    Code = 2,

    /// <summary>Picked the reservation out of the night's list.</summary>
    List = 3,
}
