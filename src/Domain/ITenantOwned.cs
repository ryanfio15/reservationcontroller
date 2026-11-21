namespace Domain;

/// <summary>
/// A row that belongs to one restaurant.
/// </summary>
/// <remarks>
/// B: Multi-tenancy — "one database, shared schema, <c>RestaurantId</c> on every tenant-owned
/// row". Implementing this is what puts an entity behind the EF global query filter, and the
/// architecture tests use it to check that none was forgotten. The filter is a default, not a
/// boundary: <c>IgnoreQueryFilters()</c> bypasses it, so the raw SQL still carries
/// <c>RestaurantId</c> explicitly and the cross-tenant tests still exist.
/// </remarks>
public interface ITenantOwned
{
    Guid RestaurantId { get; }
}
