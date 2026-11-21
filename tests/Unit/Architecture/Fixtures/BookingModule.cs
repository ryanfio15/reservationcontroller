// The module under test. Each offender below breaks the rule through a different C# construct,
// because each one compiles to a different place in the IL and an earlier version of the
// detector could only see one of them.

using Unit.Architecture.Fixtures.Modules.Restaurants.Contracts;
using Unit.Architecture.Fixtures.Modules.Restaurants.Internal;

namespace Unit.Architecture.Fixtures.Modules.Booking;

/// <summary>Uses the other module's contracts only. Must never be reported.</summary>
internal sealed class GoodCitizen(IRestaurantLookup lookup)
{
    public string Describe(int restaurantId) => lookup.NameOf(restaurantId);
}

/// <summary>
/// Breaks the rule inside an async method, so the call lands in a compiler-generated nested
/// state machine rather than in the method the author wrote.
/// </summary>
internal sealed class AsyncOffender
{
    public static async Task<string> WorkAsync()
    {
        await Task.Yield();
        return RestaurantRow.Empty().Name;
    }
}

/// <summary>Breaks the rule inside a lambda, so the call lands in a nested closure.</summary>
internal sealed class LambdaOffender
{
    public static IEnumerable<string> Work(IEnumerable<int> ids) =>
        ids.Select(_ => RestaurantRow.Empty().Name).ToList();
}

/// <summary>
/// Breaks the rule through a closed generic call, where the forbidden type appears only as the
/// method's generic argument.
/// </summary>
internal sealed class GenericOffender
{
    public static string Work() => Create<RestaurantRow>().Name;

    private static T Create<T>()
        where T : new() => new();
}

/// <summary>
/// Breaks the rule through a generic constraint, naming the forbidden type without ever calling
/// a member of it.
/// </summary>
internal sealed class ConstraintOffender<T>
    where T : IRestaurantMarker
{
    public static string Name => typeof(T).Name;
}
