namespace Domain.Reservations;

/// <summary>
/// One attempt to check a diner in. Insert-only.
/// </summary>
/// <remarks>
/// The per-device code-lookup throttle counts these rows. Append-only for the same reason as the
/// host sign-in attempts: a deletable log is a way to clear a throttle.
/// </remarks>
public sealed class RedemptionAttempt : ITenantOwned
{
    public long AttemptId { get; init; }

    public Guid RestaurantId { get; init; }

    public Guid DeviceId { get; init; }

    public RedemptionMethod Method { get; init; }

    public bool Succeeded { get; init; }

    public DateTime OccurredAtUtc { get; init; }
}
