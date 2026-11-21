namespace Domain.Reservations;

/// <summary>
/// The reservation lifecycle rules. Pure: no I/O, no clock, no database.
/// </summary>
/// <remarks>
/// <para>
/// B: Core domain logic — "The status rule is one pure function in <c>Domain</c>,
/// <c>Status(terminalEventType, redeemUntilUtc, nowUtc)</c>, and it is the only authority:
/// queries return the first two values and no SQL repeats the rule."
/// </para>
/// <para>
/// Nothing here depends on a timer. An expired ticket cannot be redeemed because the redemption
/// insert checks the window, and a no-show is derived from timestamps on every read — so the
/// optional scheduled job that writes <see cref="ReservationEventType.NoShow"/> rows can be late,
/// run twice, or never run at all without changing any answer.
/// </para>
/// </remarks>
public static class ReservationLifecycle
{
    /// <summary>
    /// A reservation's status.
    /// </summary>
    /// <param name="terminalEventType">
    /// The reservation's terminal event, or <see langword="null"/> if it has none. Must not be
    /// <see cref="ReservationEventType.Created"/>.
    /// </param>
    /// <param name="redeemUntilUtc">The end of the redemption window, from the ticket.</param>
    /// <param name="nowUtc">The current instant, from the caller's <c>TimeProvider</c>.</param>
    public static ReservationStatus Status(
        ReservationEventType? terminalEventType,
        DateTime redeemUntilUtc,
        DateTime nowUtc)
    {
        RequireUtc(nowUtc, nameof(nowUtc));
        RequireUtc(redeemUntilUtc, nameof(redeemUntilUtc));

        if (terminalEventType is null)
        {
            // "NoShow — derived once RedeemUntilUtc has passed". At exactly RedeemUntilUtc it has
            // not passed, which is also when the redemption insert still accepts the ticket; the
            // two must agree or a diner could be checked in and read as a no-show.
            return nowUtc > redeemUntilUtc
                ? ReservationStatus.NoShow
                : ReservationStatus.Confirmed;
        }

        // A terminal event is the answer, whatever the clock says afterwards: a diner who was
        // checked in does not become a no-show an hour later.
        return terminalEventType switch
        {
            ReservationEventType.CancelledByDiner => ReservationStatus.CancelledByDiner,
            ReservationEventType.CancelledByRestaurant => ReservationStatus.CancelledByRestaurant,
            ReservationEventType.Redeemed => ReservationStatus.Redeemed,
            ReservationEventType.NoShow => ReservationStatus.NoShow,

            // Created is the opening event. Reaching here means the query selected the wrong
            // row, and treating it as "no terminal event" would hide that.
            ReservationEventType.Created => throw new ArgumentOutOfRangeException(
                nameof(terminalEventType),
                terminalEventType,
                "Created is not a terminal event. Pass the reservation's terminal event, or null."),

            _ => throw new ArgumentOutOfRangeException(
                nameof(terminalEventType),
                terminalEventType,
                "Unknown reservation event type."),
        };
    }

    /// <summary>
    /// Whether <paramref name="eventType"/> ends the reservation's life.
    /// </summary>
    /// <remarks>
    /// This must agree with <c>ReservationEvent</c>'s CHECK constraint, which ties
    /// <c>IsTerminal</c> to <c>EventType</c>, and with the filtered unique index
    /// <c>UX_ReservationEvent_Terminal</c> on <c>(ReservationId) WHERE IsTerminal = 1</c> — the
    /// index is what makes "exactly one terminal event" a database guarantee rather than a
    /// convention. The integration tests assert the two definitions match.
    /// </remarks>
    public static bool IsTerminal(ReservationEventType eventType) => eventType switch
    {
        ReservationEventType.Created => false,
        ReservationEventType.CancelledByDiner => true,
        ReservationEventType.CancelledByRestaurant => true,
        ReservationEventType.Redeemed => true,
        ReservationEventType.NoShow => true,
        _ => throw new ArgumentOutOfRangeException(
            nameof(eventType),
            eventType,
            "Unknown reservation event type."),
    };

    /// <summary>
    /// Whether <paramref name="eventType"/> gives the slot's capacity back.
    /// </summary>
    /// <remarks>
    /// Only the two cancellations do. A redemption consumes the seat, and a no-show means the
    /// table was held and lost — returning capacity for either would oversell the slot.
    /// </remarks>
    public static bool ReturnsCapacity(ReservationEventType eventType) => eventType switch
    {
        ReservationEventType.CancelledByDiner => true,
        ReservationEventType.CancelledByRestaurant => true,
        ReservationEventType.Created => false,
        ReservationEventType.Redeemed => false,
        ReservationEventType.NoShow => false,
        _ => throw new ArgumentOutOfRangeException(
            nameof(eventType),
            eventType,
            "Unknown reservation event type."),
    };

    private static void RequireUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"Every instant in this system is UTC (B: Time zones), but {parameterName} has "
                + $"Kind {value.Kind}. A local or unspecified value would shift the no-show "
                + "boundary by the host's offset.",
                parameterName);
        }
    }
}
