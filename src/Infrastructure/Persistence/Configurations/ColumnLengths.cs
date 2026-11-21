namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Column lengths B does not state, kept in one place so the same kind of value is the same width
/// everywhere.
/// </summary>
/// <remarks>
/// B: Logical data model gives the lengths that matter to its own rules —
/// <c>TimeZoneId varchar(64)</c>, <c>Cuisine varchar(40)</c>, <c>binary(32)</c> for every hash —
/// and leaves the rest open. These are the choices made here; <c>docs/DECISIONS.md</c> D13
/// records why each one is where it is.
/// </remarks>
internal static class ColumnLengths
{
    /// <summary>The practical maximum for an address a mail system will accept.</summary>
    public const int Email = 256;

    public const int PersonName = 128;

    public const int Phone = 32;

    public const int RestaurantName = 200;

    public const int AddressLine = 200;

    public const int City = 100;

    public const int Region = 100;

    public const int PostalCode = 20;

    public const int Country = 100;

    /// <summary>An IANA zone name, as B states.</summary>
    public const int TimeZoneId = 64;

    public const int Cuisine = 40;

    /// <summary>A host-stand device's human label, such as "Front desk".</summary>
    public const int Label = 100;

    /// <summary>A Key Vault secret version identifier.</summary>
    public const int KeyVersion = 64;

    /// <summary>
    /// A caller-supplied idempotency key. Wide enough for a GUID in any format or a ULID, and
    /// narrow enough to stay inside an index key.
    /// </summary>
    public const int IdempotencyKey = 64;

    /// <summary>An audit action name, such as <c>HostSignInSucceeded</c>.</summary>
    public const int AuditAction = 64;

    public const int AuditTargetType = 64;

    /// <summary>
    /// An audit target's identifier as text: a GUID, or a <c>bigint</c> rendered in decimal.
    /// </summary>
    public const int AuditTargetId = 64;

    /// <summary>
    /// Deliberately small. <c>DetailJson</c> holds ids and enum values only, and a narrow column
    /// is a standing reminder that it is not somewhere to dump a request.
    /// </summary>
    public const int AuditDetailJson = 1000;
}
