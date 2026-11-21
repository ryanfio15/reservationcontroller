namespace Domain.Reservations;

/// <summary>
/// The guest's name and telephone number for a staff-created booking. Mutable.
/// </summary>
/// <remarks>
/// Deliberately kept out of the insert-only tables so it can be corrected and erased. Those
/// tables hold ids and enum values only, which is what makes erasure a matter of anonymising
/// <c>UserAccount</c> or deleting this row (B: Immutability, "No PII in insert-only tables").
/// </remarks>
public sealed class ReservationContact : ITenantOwned
{
    public Guid ReservationId { get; init; }

    public Guid RestaurantId { get; init; }

    public string GuestName { get; set; } = string.Empty;

    public string? Phone { get; set; }
}
