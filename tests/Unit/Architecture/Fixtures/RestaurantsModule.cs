// A pretend module for AssemblyDependenciesTests, shaped exactly like the real ones: a Contracts
// namespace other modules may use, and an Internal namespace they may not. Nothing here is a
// real module and no production code uses it.

namespace Unit.Architecture.Fixtures.Modules.Restaurants.Contracts;

/// <summary>The only surface another module is allowed to touch.</summary>
internal interface IRestaurantLookup
{
    string NameOf(int restaurantId);
}
