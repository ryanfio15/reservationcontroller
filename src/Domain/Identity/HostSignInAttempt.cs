namespace Domain.Identity;

/// <summary>
/// One attempt to sign in at a host stand. Insert-only.
/// </summary>
/// <remarks>
/// The lockout windows are counted from these rows, under the device's row lock. They are
/// append-only because a deletable attempt log is a way to clear a lockout.
/// </remarks>
public sealed class HostSignInAttempt
{
    public long AttemptId { get; init; }

    public Guid DeviceId { get; init; }

    /// <summary>Which host it turned out to be. Set only when the attempt succeeded.</summary>
    public Guid? MembershipId { get; init; }

    public bool Succeeded { get; init; }

    public DateTime OccurredAtUtc { get; init; }
}
