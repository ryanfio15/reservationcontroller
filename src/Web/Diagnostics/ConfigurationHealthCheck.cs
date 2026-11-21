using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Web.Configuration;

namespace Web.Diagnostics;

/// <summary>
/// The readiness check behind <c>/readyz</c>. It inspects configuration only — never the
/// database — because a probe that woke the database would defeat scale to zero and auto-pause
/// (B: Cross-cutting concerns, Compute).
/// </summary>
/// <remarks>
/// Phase 2 adds the ticket-signing key ring to this check; the structure below is where it goes.
/// </remarks>
internal sealed class ConfigurationHealthCheck(IOptions<ReservationsOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Accessing Value runs the registered validators and throws if any failed.
            _ = options.Value;
        }
        catch (OptionsValidationException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Configuration is invalid.",
                data: new Dictionary<string, object> { ["failures"] = ex.Failures.ToArray() }));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Configuration is valid."));
    }
}
