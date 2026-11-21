namespace Unit.Architecture;

/// <summary>
/// The project-level half of the architecture test required by B: Runtime, framework and
/// architecture — "forbids <c>Web.Client</c>, <c>Features.*</c> and <c>Shared.*</c> from
/// referencing <c>Domain</c> or <c>Infrastructure</c>".
/// </summary>
[Trait("Category", "Architecture")]
public sealed class ProjectBoundaryTests
{
    private static readonly string[] ServerOnlyProjects = ["Domain", "Infrastructure"];

    public static TheoryData<string> ClientProjects()
    {
        var data = new TheoryData<string>();

        foreach (var project in ProjectGraph.ProjectsMatching(IsClientSide))
        {
            data.Add(project);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ClientProjects))]
    public void Client_side_projects_do_not_reference_the_server(string project)
    {
        var reached = ProjectGraph.ReferencesOf(project);
        var forbidden = reached.Intersect(ServerOnlyProjects, StringComparer.Ordinal).ToArray();

        Assert.True(
            forbidden.Length == 0,
            $"{project} reaches {string.Join(", ", forbidden)}. The WebAssembly client, the "
            + "feature libraries and the shared libraries may use the generated API client and "
            + "the UI libraries only; shipping Domain or Infrastructure to the browser would "
            + "also ship server-side rules and SQL.");
    }

    [Fact]
    public void The_client_projects_are_actually_present()
    {
        // Guards the theory above: if the naming convention changes, the theory would silently
        // have no cases and the rule would stop being enforced.
        var found = ProjectGraph.ProjectsMatching(IsClientSide).ToArray();

        Assert.Contains("Web.Client", found);
        Assert.Contains("Features.Diner", found);
        Assert.Contains("Features.Admin", found);
        Assert.Contains("Features.Host", found);
        Assert.Contains("App.ApiClient", found);
        Assert.Contains("App.UI", found);
    }

    [Fact]
    public void Domain_references_nothing()
    {
        // Domain holds the pure rules: status, slot generation, MAC and code derivation,
        // time-zone conversion, lockout arithmetic. A reference here is how I/O creeps in.
        Assert.Empty(ProjectGraph.Direct["Domain"]);
    }

    [Fact]
    public void The_WebAssembly_client_references_only_the_API_client_and_the_UI_libraries()
    {
        // B: Runtime, framework and architecture — "the WebAssembly client can reference nothing
        // but the generated API client and UI libraries".
        var unexpected = ProjectGraph.ReferencesOf("Web.Client")
            .Where(project => !IsClientSide(project))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unexpected.Length == 0,
            $"Web.Client reaches {string.Join(", ", unexpected)}. Everything it loads is "
            + "downloaded into the browser, so it may use the feature libraries, the shared "
            + "libraries and the generated API client only.");
    }

    [Fact]
    public void No_production_project_references_the_composition_root()
    {
        // The one legitimate consumer is the integration test host: a start-up test has to boot
        // the real application through WebApplicationFactory<Program>, which needs a reference
        // to Web. Exempting the test projects is a decision taken here, in the open, rather than
        // a weakening forced on this test when phase 2's start-up test arrives.
        var offenders = ProjectGraph.Direct
            .Where(entry => !IsTestProject(entry.Key))
            .Where(entry => entry.Value.Contains("Web", StringComparer.Ordinal))
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"{string.Join(", ", offenders)} reference Web. Nothing but a test host may depend "
            + "on the composition root.");
    }

    private static bool IsTestProject(string project) =>
        project is "Unit" or "Components" or "Integration" or "E2E";

    private static bool IsClientSide(string project) =>
        project is "Web.Client"
        || project.StartsWith("Features.", StringComparison.Ordinal)
        || project.StartsWith("App.", StringComparison.Ordinal);
}
