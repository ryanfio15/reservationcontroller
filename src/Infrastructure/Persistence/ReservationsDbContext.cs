using Domain.Auditing;
using Domain.Identity;
using Domain.Reservations;
using Domain.Restaurants;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// The one database (B: Runtime, framework and architecture — "one deployable, one database").
/// </summary>
/// <remarks>
/// <para>
/// EF Core owns the schema and does the ordinary reads and writes. The three contended
/// operations — booking, redemption and host sign-in — are hand-written conditional SQL whose
/// affected row count decides the outcome, and they arrive in phases 4 and 5.
/// </para>
/// <para>
/// This context never stamps a time. Every instant comes from the caller's
/// <see cref="TimeProvider"/> and reaches SQL as <c>@NowUtc</c>; the database clock is read
/// nowhere in application logic (B: Time zones). The only exception is the temporal tables'
/// period columns, which the engine stamps itself and which nothing depends on.
/// </para>
/// </remarks>
public sealed class ReservationsDbContext(
    DbContextOptions<ReservationsDbContext> options,
    ITenantScope tenantScope)
    : DbContext(options)
{
    /// <summary>
    /// The restaurant in scope, read by the global query filters.
    /// </summary>
    /// <remarks>
    /// Exposed as a property rather than closed over as a local, because EF compiles the filter
    /// expression once per model and re-evaluates instance members of the context on each query.
    /// A captured local would freeze the first request's tenant into the model.
    /// </remarks>
    public Guid? CurrentRestaurantId => tenantScope.CurrentRestaurantId;

    // --- Identity and tenancy ---------------------------------------------------------------

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<StaffMembership> StaffMemberships => Set<StaffMembership>();

    public DbSet<HostStandDevice> HostStandDevices => Set<HostStandDevice>();

    public DbSet<HostSignInAttempt> HostSignInAttempts => Set<HostSignInAttempt>();

    public DbSet<HostAccessCode> HostAccessCodes => Set<HostAccessCode>();

    public DbSet<HostSession> HostSessions => Set<HostSession>();

    // --- Restaurants, hours and availability ------------------------------------------------

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<RestaurantCuisine> RestaurantCuisines => Set<RestaurantCuisine>();

    public DbSet<OperatingHours> OperatingHours => Set<OperatingHours>();

    public DbSet<SpecialDay> SpecialDays => Set<SpecialDay>();

    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();

    public DbSet<SlotInventory> SlotInventory => Set<SlotInventory>();

    // --- Reservations -----------------------------------------------------------------------

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<ReservationTicket> ReservationTickets => Set<ReservationTicket>();

    public DbSet<ReservationEvent> ReservationEvents => Set<ReservationEvent>();

    public DbSet<ReservationContact> ReservationContacts => Set<ReservationContact>();

    public DbSet<RedemptionAttempt> RedemptionAttempts => Set<RedemptionAttempt>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationsDbContext).Assembly);

        ApplyTenantFilters(modelBuilder);
    }

    /// <summary>
    /// The global query filters (B: Multi-tenancy).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written out one table at a time rather than applied by reflection, so that a reviewer can
    /// see which tables are filtered and on what. <c>EveryTenantOwnedEntityIsFiltered</c> in the
    /// unit tests is what catches a table added later and not listed here.
    /// </para>
    /// <para>
    /// Each filter closes over <see cref="CurrentRestaurantId"/>, which EF turns into a query
    /// parameter read at execution time — so one cached model serves every tenant.
    /// </para>
    /// <para>
    /// Remember what this is and is not. It removes a whole class of forgotten-<c>Where</c> bugs
    /// from ordinary EF reads. It is not the tenant boundary: <c>IgnoreQueryFilters()</c> turns
    /// it off, the hand-written SQL does not go through it at all, and an unscoped request sees
    /// everything by design, because diner search spans restaurants. The boundary is the
    /// authorization policies and the explicit <c>RestaurantId</c> predicates, which the
    /// cross-tenant tests exercise.
    /// </para>
    /// </remarks>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        // The tenant itself. Staff in scope see their own restaurant; diner search is unscoped.
        modelBuilder.Entity<Restaurant>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);

        modelBuilder.Entity<StaffMembership>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<HostStandDevice>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<HostAccessCode>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<HostSession>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);

        modelBuilder.Entity<RestaurantCuisine>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<OperatingHours>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<SpecialDay>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<AvailabilityRule>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<SlotInventory>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);

        modelBuilder.Entity<Reservation>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<ReservationTicket>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<ReservationEvent>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<ReservationContact>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);
        modelBuilder.Entity<RedemptionAttempt>()
            .HasQueryFilter(e => CurrentRestaurantId == null || e.RestaurantId == CurrentRestaurantId);

        // AuditLog's RestaurantId is nullable, because onboarding a restaurant and erasing an
        // account belong to no single tenant. Those rows stay visible when a scope is set: they
        // are the ones a support query most needs, and hiding them would make the audit trail
        // look complete while missing exactly the cross-tenant actions.
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(e =>
                CurrentRestaurantId == null
                || e.RestaurantId == null
                || e.RestaurantId == CurrentRestaurantId);

        // UserAccount and HostSignInAttempt have no RestaurantId and so cannot be filtered.
        // A user belongs to no restaurant — a diner books across many — and a sign-in attempt is
        // keyed only by its device. Reads of both carry explicit predicates instead.
    }
}
