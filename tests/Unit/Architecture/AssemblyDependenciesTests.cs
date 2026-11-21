namespace Unit.Architecture;

/// <summary>
/// Tests the module-boundary detector itself, against fixtures that really do break the rule.
/// </summary>
/// <remarks>
/// <see cref="ModuleBoundaryTests"/> cannot do this job: until phase 1 fills
/// <c>Infrastructure.Modules</c>, every case in it passes having examined nothing. A detector
/// that has never caught a violation is not known to work, and each offender here uses a
/// different C# construct because each compiles to a different place in the IL.
/// </remarks>
[Trait("Category", "Architecture")]
public sealed class AssemblyDependenciesTests
{
    private const string FixtureRoot = "Unit.Architecture.Fixtures.Modules";
    private const string Booking = $"{FixtureRoot}.Booking";
    private const string Restaurants = $"{FixtureRoot}.Restaurants";
    private const string RestaurantsContracts = $"{Restaurants}.Contracts";

    private static readonly Lazy<AssemblyDependencies> Self =
        new(() => AssemblyDependencies.Of(typeof(AssemblyDependenciesTests).Assembly),
            isThreadSafe: true);

    [Fact]
    public void Every_shape_of_cross_module_reference_is_detected()
    {
        var offenders = OffendingTypes();

        Assert.Contains($"{Booking}.AsyncOffender", offenders);
        Assert.Contains($"{Booking}.LambdaOffender", offenders);
        Assert.Contains($"{Booking}.GenericOffender", offenders);
        Assert.Contains($"{Booking}.ConstraintOffender`1", offenders);
    }

    [Fact]
    public void A_type_that_uses_only_the_other_modules_contracts_is_not_reported()
    {
        Assert.DoesNotContain($"{Booking}.GoodCitizen", OffendingTypes());
    }

    [Fact]
    public void The_offenders_are_the_only_types_reported()
    {
        string[] expected =
        [
            $"{Booking}.AsyncOffender",
            $"{Booking}.ConstraintOffender`1",
            $"{Booking}.GenericOffender",
            $"{Booking}.LambdaOffender",
        ];

        Assert.Equal(expected, OffendingTypes());
    }

    [Fact]
    public void Nested_types_keep_their_declaring_types_namespace()
    {
        // The bug this pins: Cecil reports no namespace for a nested type, so naming one from
        // Namespace alone collapsed every closure in the assembly to "<>c" — colliding keys and
        // a name that matches no namespace rule.
        var nested = Self.Value.TypesIn(Booking)
            .Where(type => type.Contains('+', StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(nested);
        Assert.All(nested, type => Assert.StartsWith(Booking + ".", type, StringComparison.Ordinal));

        // And they are distinct, rather than several entries sharing a bare compiler-generated
        // name.
        Assert.Equal(nested.Length, nested.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void A_namespace_is_matched_by_whole_segments()
    {
        // "Booking" must not match "BookingLegacy". Asserted through the public behaviour: the
        // fixtures contain no such namespace, so a prefix query for one finds nothing.
        Assert.Empty(Self.Value.TypesIn($"{FixtureRoot}.Book"));
        Assert.NotEmpty(Self.Value.TypesIn(Booking));
    }

    /// <summary>
    /// The source types reported as offenders, with compiler-generated nested types folded into
    /// the type the author wrote and the dependency list stripped off.
    /// </summary>
    private static IReadOnlyList<string> OffendingTypes() =>
        [.. Self.Value
            .Offenders(
                namespacePrefix: Booking,
                forbiddenPrefix: Restaurants,
                allowedPrefix: RestaurantsContracts)
            .Select(entry => entry.Split(" -> ", StringSplitOptions.None)[0])
            .Select(type => type.Split('+', StringSplitOptions.None)[0])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
}
