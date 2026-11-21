using System.Reflection;

namespace Infrastructure;

/// <summary>
/// Anchor for assembly scanning and for the architecture tests.
/// </summary>
/// <remarks>
/// The modules named in B: Runtime, framework and architecture live under
/// <c>Infrastructure.Modules.&lt;Module&gt;</c> — Identity, Restaurants, Booking, Ticketing — and
/// each exposes what other modules may use under <c>Infrastructure.Modules.&lt;Module&gt;.Contracts</c>.
/// The architecture tests enforce that boundary.
/// </remarks>
public static class InfrastructureAssembly
{
    public static Assembly Value => typeof(InfrastructureAssembly).Assembly;
}
