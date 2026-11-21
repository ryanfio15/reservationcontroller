using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Web.Api;
using Web.Configuration;
using Web.Diagnostics;

// `App` is the root namespace of the shared libraries (App.Core, App.UI, ...), so the root
// component is named in full wherever it is used from this project.
using RootComponent = Web.Components.App;

var builder = WebApplication.CreateBuilder(args);

// --- services ---------------------------------------------------------------------------------
//
// Nothing here may open a database connection. Serving `/`, `/healthz` and `/readyz` must not
// touch the database at all, so that probes and the landing page cannot keep a paused database
// awake (B: Compute, Cross-cutting concerns). A start-up test enforces this from phase 2.

builder.Services
    .AddOptions<ReservationsOptions>()
    .Bind(builder.Configuration.GetSection(ReservationsOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => o.Validate().Count == 0, "Reservations configuration is invalid.")
    .ValidateOnStart();

// Errors leave this host as RFC 9457 problem details, including from the Minimal API groups.
builder.Services.AddProblemDetails();

builder.Services
    .AddHealthChecks()
    .AddCheck<ConfigurationHealthCheck>("configuration", tags: ["ready"]);

// OpenAPI 3.1 document for /api/v1, from which the typed client is generated (phase 7).
builder.Services.AddOpenApi("v1", options =>
    options.AddDocumentTransformer<OpenApiDocumentDetails>());

builder.Services
    .AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// --- pipeline ---------------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// --- endpoints --------------------------------------------------------------------------------

// Liveness: the process answered. No checks run, so this cannot fail for a downstream reason.
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();

// Readiness: configuration and (from phase 2) keys. Never the database.
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
})
    .AllowAnonymous();

// The API surface. Each module maps its own endpoint group under /api/v1 from phase 3; the
// document at /openapi/v1.json describes whatever is mapped, and is empty until then.
app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<RootComponent>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Web.Client._Imports).Assembly);

app.Run();
