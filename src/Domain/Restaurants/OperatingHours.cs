namespace Domain.Restaurants;

/// <summary>
/// One opening period on one weekday, in the restaurant's local wall-clock time.
/// </summary>
/// <remarks>
/// Several rows for the same day allow split service — lunch and dinner with a closed afternoon
/// between. Hours may not cross midnight in the prototype, so <c>OpenTime &lt; CloseTime</c> is
/// enforced (B: Core domain logic).
/// </remarks>
public sealed class OperatingHours : ITenantOwned
{
    /// <summary>
    /// A surrogate key. B lists no key for this table, and the natural one would have to be
    /// <c>(RestaurantId, DayOfWeek, OpenTime)</c> — which a temporal table cannot use, since the
    /// history side needs a stable identity for a row whose times change.
    /// </summary>
    public Guid OperatingHoursId { get; init; }

    public Guid RestaurantId { get; init; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }
}
