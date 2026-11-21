namespace Domain.Restaurants;

/// <summary>
/// A restaurant. The tenant every other row belongs to.
/// </summary>
/// <remarks>
/// A <c>Business</c> table grouping several restaurants under one owner is a Production item
/// (B: Logical data model); nothing in the prototype would read it, so it does not exist.
/// </remarks>
public sealed class Restaurant
{
    public Guid RestaurantId { get; init; }

    public string Name { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    /// <summary>Part of the search index <c>(IsListed, City, Name)</c>: search is a name prefix within a city.</summary>
    public string City { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    /// <summary>Held for the Cut-first "near me" bounding box. Nothing reads it yet.</summary>
    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    /// <summary>
    /// The restaurant's IANA time zone, for example <c>America/New_York</c>. Opening hours are
    /// local wall-clock times converted once, when slots are generated, and the service date is
    /// the local date (B: Time zones).
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>How long after the booked time a diner may still be checked in.</summary>
    public int GraceMinutes { get; set; }

    /// <summary>How far ahead diners may book, and how far ahead slots are materialised.</summary>
    public int BookingHorizonDays { get; set; }

    /// <summary>Failed host sign-ins inside the window before a device locks.</summary>
    public int HostLockThreshold { get; set; } = 5;

    /// <summary>Failed host sign-ins inside the window before a device is suspended until a manager reinstates it.</summary>
    public int HostSuspendThreshold { get; set; } = 10;

    /// <summary>Whether the restaurant appears in diner search.</summary>
    public bool IsListed { get; set; }

    public byte[]? RowVersion { get; set; }
}
