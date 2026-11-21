namespace Infrastructure.Persistence;

/// <summary>
/// The restaurant the current request is acting within, if any.
/// </summary>
/// <remarks>
/// <para>
/// B: Multi-tenancy — "one database, shared schema, <c>RestaurantId</c> on every tenant-owned
/// row", and "query filters are a default, not a boundary". This is what the global query filters
/// read. Phase 2 implements it from the caller's verified staff membership; never from a route
/// value, a header or a request body, because then a caller could choose their own tenant.
/// </para>
/// <para>
/// A request can legitimately be unscoped: diner search and availability reads span restaurants.
/// That is why <see cref="CurrentRestaurantId"/> is nullable and why an unscoped request sees
/// every tenant's rows — and in turn why the raw SQL still carries <c>RestaurantId</c> as an
/// explicit predicate, and why the cross-tenant authorization tests exist. The filter catches
/// the forgotten <c>Where</c>; it is not the thing standing between two restaurants.
/// </para>
/// </remarks>
public interface ITenantScope
{
    /// <summary>The restaurant in scope, or <see langword="null"/> for a cross-tenant request.</summary>
    Guid? CurrentRestaurantId { get; }
}
