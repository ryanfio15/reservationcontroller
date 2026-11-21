namespace Domain.Restaurants;

/// <summary>
/// How many parties of a given size may be seated, how often, on one weekday.
/// </summary>
/// <remarks>
/// Rules materialise into <c>SlotInventory</c> rows, one per (slot, party band). Two rules for
/// the same day whose time ranges overlap may not have overlapping party bands: two such rules
/// sharing a <c>PartyMin</c> would collide on the slot unique key (B: Core domain logic).
/// </remarks>
public sealed class AvailabilityRule : ITenantOwned
{
    public Guid RuleId { get; init; }

    public Guid RestaurantId { get; init; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    /// <summary>Minutes between slot starts.</summary>
    public int SlotMinutes { get; set; }

    public int PartyMin { get; set; }

    public int PartyMax { get; set; }

    /// <summary>How many parties in this band may be seated in one slot.</summary>
    public int CapacityPerSlot { get; set; }

    public byte[]? RowVersion { get; set; }
}
