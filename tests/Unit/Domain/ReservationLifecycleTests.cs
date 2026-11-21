using Domain.Reservations;

namespace Unit.Domain;

/// <summary>
/// The status rule from B: Core domain logic, "Lifecycle" — one pure function,
/// <c>Status(terminalEventType, redeemUntilUtc, nowUtc)</c>, which is the only authority on a
/// reservation's status. No status is stored, no view computes it and no SQL repeats it.
/// </summary>
[Trait("Category", "Domain")]
public sealed class ReservationLifecycleTests
{
    private static readonly DateTime RedeemUntil = new(2026, 11, 21, 20, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void A_reservation_with_no_terminal_event_before_its_window_closes_is_confirmed()
    {
        var status = ReservationLifecycle.Status(
            terminalEventType: null,
            redeemUntilUtc: RedeemUntil,
            nowUtc: RedeemUntil.AddMinutes(-1));

        Assert.Equal(ReservationStatus.Confirmed, status);
    }

    [Fact]
    public void A_reservation_with_no_terminal_event_is_a_no_show_once_its_window_has_passed()
    {
        // "NoShow — derived once RedeemUntilUtc has passed". Nothing writes this to become true;
        // the same timestamps give the same answer on every read, which is what lets the
        // scheduled job be optional.
        var status = ReservationLifecycle.Status(
            terminalEventType: null,
            redeemUntilUtc: RedeemUntil,
            nowUtc: RedeemUntil.AddTicks(1));

        Assert.Equal(ReservationStatus.NoShow, status);
    }

    [Fact]
    public void The_instant_the_window_closes_is_still_confirmed()
    {
        // "once RedeemUntilUtc has passed" — at exactly RedeemUntilUtc it has not passed, and
        // the redemption insert still accepts the ticket, so the two must agree.
        var status = ReservationLifecycle.Status(
            terminalEventType: null,
            redeemUntilUtc: RedeemUntil,
            nowUtc: RedeemUntil);

        Assert.Equal(ReservationStatus.Confirmed, status);
    }

    [Theory]
    [InlineData(ReservationEventType.CancelledByDiner, ReservationStatus.CancelledByDiner)]
    [InlineData(ReservationEventType.CancelledByRestaurant, ReservationStatus.CancelledByRestaurant)]
    [InlineData(ReservationEventType.Redeemed, ReservationStatus.Redeemed)]
    [InlineData(ReservationEventType.NoShow, ReservationStatus.NoShow)]
    public void A_terminal_event_decides_the_status(
        ReservationEventType terminal,
        ReservationStatus expected)
    {
        var status = ReservationLifecycle.Status(terminal, RedeemUntil, RedeemUntil.AddMinutes(-1));

        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(ReservationEventType.CancelledByDiner, ReservationStatus.CancelledByDiner)]
    [InlineData(ReservationEventType.CancelledByRestaurant, ReservationStatus.CancelledByRestaurant)]
    [InlineData(ReservationEventType.Redeemed, ReservationStatus.Redeemed)]
    public void A_terminal_event_is_not_overtaken_by_the_clock(
        ReservationEventType terminal,
        ReservationStatus expected)
    {
        // A redeemed diner who ate at the restaurant must not become a no-show an hour later,
        // and a cancellation does not turn into one either.
        var status = ReservationLifecycle.Status(terminal, RedeemUntil, RedeemUntil.AddDays(7));

        Assert.Equal(expected, status);
    }

    [Fact]
    public void Created_is_not_a_terminal_event_and_may_not_be_passed_as_one()
    {
        // Created is the opening event, never the terminal one. Passing it is a caller bug: the
        // query feeding this function must select the terminal event, and silently treating
        // Created as "no terminal event" would hide that.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReservationLifecycle.Status(
                ReservationEventType.Created,
                RedeemUntil,
                RedeemUntil.AddMinutes(-1)));
    }

    [Fact]
    public void An_unknown_event_type_is_rejected_rather_than_guessed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReservationLifecycle.Status(
                (ReservationEventType)99,
                RedeemUntil,
                RedeemUntil.AddMinutes(-1)));
    }

    [Fact]
    public void The_clock_must_be_UTC()
    {
        // Every instant in this system is UTC (B: Time zones). A local DateTime reaching here
        // would silently shift the no-show boundary by the host's offset.
        Assert.Throws<ArgumentException>(
            () => ReservationLifecycle.Status(
                null,
                RedeemUntil,
                DateTime.SpecifyKind(RedeemUntil, DateTimeKind.Local)));
    }

    [Theory]
    [InlineData(ReservationEventType.Created, false)]
    [InlineData(ReservationEventType.CancelledByDiner, true)]
    [InlineData(ReservationEventType.CancelledByRestaurant, true)]
    [InlineData(ReservationEventType.Redeemed, true)]
    [InlineData(ReservationEventType.NoShow, true)]
    public void IsTerminal_matches_the_database_CHECK_constraint(
        ReservationEventType eventType,
        bool expected)
    {
        // ReservationEvent has a CHECK tying IsTerminal to EventType, and a filtered unique
        // index on (ReservationId) WHERE IsTerminal = 1. If this function and that constraint
        // ever disagree, inserts start failing at random.
        Assert.Equal(expected, ReservationLifecycle.IsTerminal(eventType));
    }

    [Fact]
    public void Cancellation_returns_capacity_and_the_other_terminal_events_do_not()
    {
        // "CancelledByDiner | CancelledByRestaurant — terminal, returns capacity".
        Assert.True(ReservationLifecycle.ReturnsCapacity(ReservationEventType.CancelledByDiner));
        Assert.True(ReservationLifecycle.ReturnsCapacity(ReservationEventType.CancelledByRestaurant));

        Assert.False(ReservationLifecycle.ReturnsCapacity(ReservationEventType.Redeemed));
        Assert.False(ReservationLifecycle.ReturnsCapacity(ReservationEventType.NoShow));
        Assert.False(ReservationLifecycle.ReturnsCapacity(ReservationEventType.Created));
    }

    [Fact]
    public void The_event_type_numbers_are_the_ones_the_schema_stores()
    {
        // Stored as tinyint. Renumbering the enum would silently reinterpret every existing row.
        Assert.Equal(1, (byte)ReservationEventType.Created);
        Assert.Equal(2, (byte)ReservationEventType.CancelledByDiner);
        Assert.Equal(3, (byte)ReservationEventType.CancelledByRestaurant);
        Assert.Equal(4, (byte)ReservationEventType.Redeemed);
        Assert.Equal(5, (byte)ReservationEventType.NoShow);
    }
}
