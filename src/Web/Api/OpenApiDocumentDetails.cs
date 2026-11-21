using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Web.Configuration;

namespace Web.Api;

/// <summary>
/// Fills in the document-level details of <c>/openapi/v1.json</c>.
/// </summary>
/// <remarks>
/// The typed client in <c>Shared/App.ApiClient</c> is generated from this document (phase 7), so
/// its title becomes the client's name and its server entry becomes the client's base address.
/// Without this the title would be the host assembly's name, "Web".
/// </remarks>
internal sealed class OpenApiDocumentDetails(IOptions<ReservationsOptions> options)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Reservations API",
            Version = "v1",
            Description =
                "Table reservations: diner search and booking, host stand check-in, and "
                + "restaurant administration.",
        };

        var baseUrl = options.Value.PublicBaseUrl;
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            // Pinned to the deployment's own host. Left alone locally, where the document's
            // relative paths already resolve against whatever host served it.
            document.Servers = [new OpenApiServer { Url = baseUrl.TrimEnd('/') }];
        }

        return Task.CompletedTask;
    }
}
