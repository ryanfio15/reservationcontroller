namespace Domain.Identity;

/// <summary>
/// A host's 4-digit sign-in code, stored only as a keyed HMAC.
/// </summary>
/// <remarks>
/// A 4-digit value cannot be protected by hashing alone: 10,000 candidates fall instantly to
/// whoever holds the table (B: Security of stored data). What protects it is the Key Vault-held
/// HMAC key, which a database-only leak does not include, plus device pairing, per-device
/// throttling and audit. A pepper rotation reissues and revokes every code, so the active rows
/// in one restaurant all share a single <see cref="KeyVersion"/>.
/// </remarks>
public sealed class HostAccessCode : ITenantOwned
{
    public Guid HostCodeId { get; init; }

    public Guid RestaurantId { get; init; }

    public Guid MembershipId { get; init; }

    /// <summary>HMAC of the code under the key named by <see cref="KeyVersion"/>. Never the code.</summary>
    public byte[] CodeLookupHmac { get; set; } = [];

    public string KeyVersion { get; set; } = string.Empty;

    public DateTime? RevokedAtUtc { get; set; }
}
