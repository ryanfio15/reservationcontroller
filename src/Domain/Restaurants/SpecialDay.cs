namespace Domain.Restaurants;

/// <summary>
/// An override for one local date: closed, or open at different times.
/// </summary>
/// <remarks>
/// Keyed on <c>(RestaurantId, LocalDate)</c>, so a date has at most one override and the weekly
/// pattern is either used or replaced, never merged.
/// </remarks>
public sealed class SpecialDay : ITenantOwned
{
    public Guid RestaurantId { get; init; }

    /// <summary>The restaurant's local date, not a UTC date.</summary>
    public DateOnly LocalDate { get; init; }

    public bool IsClosed { get; set; }

    /// <summary>Null when <see cref="IsClosed"/>, or when only one end of the day moves.</summary>
    public TimeOnly? OpenTime { get; set; }

    public TimeOnly? CloseTime { get; set; }
}
