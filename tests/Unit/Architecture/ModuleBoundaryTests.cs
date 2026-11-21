using Domain;
using Infrastructure;

namespace Unit.Architecture;

/// <summary>
/// The namespace-level half of the architecture test required by B: Runtime, framework and
/// architecture — "forbids cross-module references except through each module's
/// <c>Contracts</c> namespace".
/// </summary>
/// <remarks>
/// The module namespaces fill up from phase 1. While a module has no types its pair cases pass
/// with nothing to check; <see cref="Module_types_sit_inside_one_of_the_four_modules"/> is what
/// stops that from becoming permanent silence, because it fails the moment a type lands under
/// <c>Infrastructure.Modules</c> outside an agreed module.
/// </remarks>
[Trait("Category", "Architecture")]
public sealed class ModuleBoundaryTests
{
    private const string ModulesRoot = "Infrastructure.Modules";

    private static readonly string[] Modules = ["Identity", "Restaurants", "Booking", "Ticketing"];

    private static readonly Lazy<AssemblyDependencies> Infrastructure =
        new(() => AssemblyDependencies.Of(InfrastructureAssembly.Value), isThreadSafe: true);

    private static readonly Lazy<AssemblyDependencies> DomainIl =
        new(() => AssemblyDependencies.Of(DomainAssembly.Value), isThreadSafe: true);

    public static TheoryData<string, string> ModulePairs()
    {
        var data = new TheoryData<string, string>();

        foreach (var module in Modules)
        {
            foreach (var other in Modules.Where(m => !string.Equals(m, module, StringComparison.Ordinal)))
            {
                data.Add(module, other);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void A_module_reaches_another_module_only_through_its_Contracts(string module, string other)
    {
        var offenders = Infrastructure.Value.Offenders(
            namespacePrefix: $"{ModulesRoot}.{module}",
            forbiddenPrefix: $"{ModulesRoot}.{other}",
            allowedPrefix: $"{ModulesRoot}.{other}.Contracts");

        Assert.True(
            offenders.Count == 0,
            $"Module '{module}' reaches into '{other}' past its Contracts namespace:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders)
            + Environment.NewLine
            + "Modules talk through contracts so the monolith can be split later without "
            + "unpicking every call site.");
    }

    [Fact]
    public void Module_types_sit_inside_one_of_the_four_modules()
    {
        var allowed = Modules.Select(m => $"{ModulesRoot}.{m}.").ToArray();

        var strays = Infrastructure.Value.TypesIn(ModulesRoot)
            .Where(type => !allowed.Any(prefix => type.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            strays.Length == 0,
            $"These types sit under {ModulesRoot} but not inside one of the four modules "
            + $"({string.Join(", ", Modules)}):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, strays));
    }

    [Fact]
    public void Domain_depends_on_no_infrastructure()
    {
        var forbidden = new[]
        {
            "Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Data.SqlClient",
            "System.Data",
            "System.Net.Http",
        };

        var offenders = forbidden
            .SelectMany(prefix => DomainIl.Value.Offenders(namespacePrefix: "Domain", forbiddenPrefix: prefix))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Domain holds the pure rules — status, slot generation, MAC and code derivation, "
            + "time-zone conversion, lockout arithmetic — and must stay free of I/O:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    [Theory]
    [InlineData("Domain")]
    [InlineData("Infrastructure")]
    public void No_server_code_reads_the_ambient_clock(string assemblyName)
    {
        // A2 and A3: time comes from an injected TimeProvider, and the SQL statements take
        // @NowUtc from the caller rather than reading a clock. The ambient clock is what makes
        // the DST, lockout and no-show rules untestable, so it is banned outright.
        var assembly = assemblyName switch
        {
            "Domain" => DomainAssembly.Value,
            "Infrastructure" => InfrastructureAssembly.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(assemblyName), assemblyName, null),
        };

        var callers = AssemblyDependencies.CallersOf(
            assembly,
            "System.DateTime::get_UtcNow",
            "System.DateTime::get_Now",
            "System.DateTime::get_Today",
            "System.DateTimeOffset::get_UtcNow",
            "System.DateTimeOffset::get_Now",
            "System.Environment::get_TickCount",
            "System.Environment::get_TickCount64");

        Assert.True(
            callers.Count == 0,
            $"{assemblyName} reads the ambient clock. Inject TimeProvider instead:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, callers));
    }
}
