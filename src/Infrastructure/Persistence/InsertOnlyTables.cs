namespace Infrastructure.Persistence;

/// <summary>
/// The tables that may only ever be inserted into.
/// </summary>
/// <remarks>
/// <para>
/// B: Immutability names these six for the prototype's levels 1 and 2: the runtime role holds
/// only <c>INSERT, SELECT</c> on them with <c>DENY UPDATE, DELETE</c>, and an
/// <c>INSTEAD OF UPDATE, DELETE</c> trigger on each one throws. Level 1 stops a compromised or
/// buggy application and SQL injection; level 2 stops a mistake by the migration identity or a
/// human at a query window.
/// </para>
/// <para>
/// This list is the single source of truth. The grants migration, the triggers migration and the
/// integration tests that prove an <c>UPDATE</c> fails all read it, so a seventh insert-only
/// table cannot be added to one of the three and forgotten in the others.
/// </para>
/// </remarks>
public static class InsertOnlyTables
{
    /// <summary>The table names, in the order the migrations apply them.</summary>
    public static readonly IReadOnlyList<string> Names =
    [
        "Reservation",
        "ReservationTicket",
        "ReservationEvent",
        "RedemptionAttempt",
        "HostSignInAttempt",
        "AuditLog",
    ];

    /// <summary>
    /// The name given to the <c>INSTEAD OF UPDATE, DELETE</c> trigger on
    /// <paramref name="table"/>.
    /// </summary>
    /// <remarks>
    /// EF Core must be told a table has a trigger, or it writes rows with an <c>OUTPUT</c> clause
    /// that SQL Server rejects on such a table. The name is derived rather than written out
    /// twice, so the mapping and the migration cannot drift apart.
    /// </remarks>
    public static string TriggerName(string table) => $"TR_{table}_InsertOnly";
}
