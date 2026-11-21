namespace Domain.Restaurants;

/// <summary>
/// A cuisine tag. The filter that uses these is Cut-first; the tags are stored regardless so the
/// demo data is realistic.
/// </summary>
public sealed class RestaurantCuisine : ITenantOwned
{
    public Guid RestaurantId { get; init; }

    public string Cuisine { get; init; } = string.Empty;
}
