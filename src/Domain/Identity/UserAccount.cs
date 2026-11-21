namespace Domain.Identity;

/// <summary>
/// A person who signs in, diner or staff. Created on first sign-in.
/// </summary>
/// <remarks>
/// No password, token or secret is stored here: Microsoft Entra External ID holds credentials
/// (B: Security of stored data). The row is keyed on the <c>oid</c> and <c>tid</c> claims rather
/// than on an email, because an email can be reassigned and a display name can change.
/// </remarks>
public sealed class UserAccount
{
    public Guid UserId { get; init; }

    /// <summary>The identity provider's <c>oid</c> claim.</summary>
    public Guid ExternalObjectId { get; init; }

    /// <summary>
    /// The identity provider's <c>tid</c> claim. Part of the key, because an object id is only
    /// unique within its tenant.
    /// </summary>
    public Guid ExternalTenantId { get; init; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Sessions issued before this instant are rejected. Set on sign-out-everywhere, on role
    /// changes and on erasure, which is how a cookie is revoked without server-side session
    /// state.
    /// </summary>
    public DateTime? SessionsValidFromUtc { get; set; }

    /// <summary>
    /// When the account was erased. The row stays, anonymised, because insert-only reservation
    /// rows reference it (B: Security of stored data, "PII minimisation and erasure").
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
