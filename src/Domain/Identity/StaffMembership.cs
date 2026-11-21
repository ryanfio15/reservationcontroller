namespace Domain.Identity;

/// <summary>
/// One person's role at one restaurant.
/// </summary>
/// <remarks>
/// <see cref="UserId"/> is null for a code-only host, who signs in at a paired device with a
/// 4-digit code and never has an account. Owners and managers are bound to their account on
/// first sign-in with the invited, verified email.
/// </remarks>
public sealed class StaffMembership : ITenantOwned
{
    public Guid MembershipId { get; init; }

    public Guid RestaurantId { get; init; }

    /// <summary>Null until an invited owner or manager signs in; always null for a code-only host.</summary>
    public Guid? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Where the invitation went. Matched against the verified email at first sign-in.</summary>
    public string? InviteEmail { get; set; }

    public StaffRole Role { get; set; }

    public MembershipStatus Status { get; set; }

    /// <summary>
    /// Optimistic concurrency. The owner guard reads a membership, decides, and writes; without
    /// this, two managers acting at once could between them remove the last owner.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
