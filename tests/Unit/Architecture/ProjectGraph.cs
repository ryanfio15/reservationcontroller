using System.Xml.Linq;

namespace Unit.Architecture;

/// <summary>
/// The solution's project-reference graph, read from the <c>.csproj</c> files on disk.
/// </summary>
/// <remarks>
/// Reading the project files rather than the compiled assemblies is deliberate: the rules in
/// B: Runtime, framework and architecture are stated about projects, a forbidden reference is
/// caught whether or not any code uses it yet, and the check stays instant.
/// </remarks>
internal static class ProjectGraph
{
    private static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> Graph =
        new(Build, isThreadSafe: true);

    /// <summary>Project name to the names of the projects it references directly.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> Direct => Graph.Value;

    /// <summary>Project name to every project it reaches, directly or transitively.</summary>
    internal static IReadOnlyList<string> ReferencesOf(string project)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(project);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!Direct.TryGetValue(current, out var references))
            {
                continue;
            }

            foreach (var reference in references)
            {
                if (seen.Add(reference))
                {
                    queue.Enqueue(reference);
                }
            }
        }

        return [.. seen];
    }

    internal static IEnumerable<string> ProjectsMatching(Func<string, bool> predicate) =>
        Direct.Keys.Where(predicate);

    private static Dictionary<string, IReadOnlyList<string>> Build()
    {
        var root = RepositoryRoot();
        var graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(file);
            var references = XDocument.Load(file)
                .Descendants("ProjectReference")
                .Select(e => (string?)e.Attribute("Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', '/')))
                .Order(StringComparer.Ordinal)
                .ToArray();

            graph[name] = references;
        }

        Assert.True(
            graph.Count > 0,
            $"No project files were found under '{root}'. The architecture tests cannot run.");

        return graph;
    }

    /// <summary>
    /// Walks up from the test assembly until the directory holding the solution file is found,
    /// so the tests do not depend on where the runner was started.
    /// </summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.slnx").Any() || directory.EnumerateFiles("*.sln").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No solution file was found above '{AppContext.BaseDirectory}'.");
    }
}
