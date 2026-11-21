namespace Web.Configuration;

/// <summary>
/// Options for the host as a whole. Bound from the <c>Reservations</c> configuration section and
/// validated at start-up (B: Cross-cutting concerns, "Options validated at start-up"), so a
/// malformed deployment fails fast instead of failing on a diner's first request.
/// </summary>
public sealed class ReservationsOptions
{
    public const string SectionName = "Reservations";

    /// <summary>
    /// The absolute base URL browsers reach this deployment on, used as the server entry in the
    /// OpenAPI document so a generated client points at the right host. Left empty locally,
    /// where the document's relative paths are correct already.
    /// </summary>
    /// <remarks>
    /// Validated by <see cref="Validate"/> rather than by <c>[Url]</c>, which rejects the empty
    /// string that <c>appsettings.json</c> carries as a placeholder.
    /// </remarks>
    public string? PublicBaseUrl { get; init; }

    /// <summary>
    /// Every problem with these values, named one by one. Run by
    /// <see cref="ReservationsOptionsValidator"/> at start-up and by the readiness check.
    /// </summary>
    internal IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (!string.IsNullOrWhiteSpace(PublicBaseUrl))
        {
            if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri))
            {
                problems.Add($"{SectionName}:{nameof(PublicBaseUrl)} is not an absolute URL.");
            }
            else if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                problems.Add(
                    $"{SectionName}:{nameof(PublicBaseUrl)} must use http or https, not '{uri.Scheme}'.");
            }
        }

        return problems;
    }
}
