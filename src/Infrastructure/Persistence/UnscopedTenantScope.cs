namespace Infrastructure.Persistence;

/// <summary>
/// A scope that is never set, for the paths that are legitimately cross-tenant: diner search,
/// availability reads, the migration job and the optional scheduled job.
/// </summary>
/// <remarks>
/// Registered as the default so that phase 1 — which has no sign-in yet — is explicit about
/// being unscoped, rather than quietly resolving a scope that happens to be empty. Phase 2
/// replaces the registration for request paths that carry a staff membership.
/// </remarks>
public sealed class UnscopedTenantScope : ITenantScope
{
    public Guid? CurrentRestaurantId => null;
}
