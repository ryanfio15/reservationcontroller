namespace Unit.Architecture.Fixtures.Modules.Restaurants.Internal;

/// <summary>Inside the module. Reaching this from another module is the violation.</summary>
internal sealed class RestaurantRow
{
    public int RestaurantId { get; init; }

    public string Name { get; init; } = string.Empty;

    public static RestaurantRow Empty() => new();
}

/// <summary>Named only by a generic constraint, in ConstraintOffender.</summary>
internal interface IRestaurantMarker;
