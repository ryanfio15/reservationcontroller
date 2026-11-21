using Microsoft.Extensions.Options;

namespace Web.Configuration;

/// <summary>
/// Reports exactly which configuration values are wrong, rather than that something is.
/// </summary>
/// <remarks>
/// A deployment fails at start-up on bad configuration (B: Cross-cutting concerns). The person
/// reading that failure is looking at container logs, so the message has to name the setting.
/// </remarks>
internal sealed class ReservationsOptionsValidator : IValidateOptions<ReservationsOptions>
{
    public ValidateOptionsResult Validate(string? name, ReservationsOptions options)
    {
        var problems = options.Validate();

        return problems.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(problems);
    }
}
