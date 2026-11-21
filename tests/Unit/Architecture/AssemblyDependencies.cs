using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unit.Architecture;

/// <summary>
/// Every type a type actually uses, read from IL.
/// </summary>
/// <remarks>
/// Namespace-prefix matching cannot express the rule in B: Runtime, framework and architecture,
/// because <c>Modules.Booking.Contracts</c> sits inside <c>Modules.Booking</c>: a prefix rule
/// that forbids the module also forbids its contracts, and one that allows the contracts allows
/// the module. Reading the real dependency list makes "anything under the module except its
/// Contracts namespace" exact, and it catches a dependency used only inside a method body.
/// </remarks>
internal sealed class AssemblyDependencies
{
    private readonly Dictionary<string, HashSet<string>> dependencies;

    private AssemblyDependencies(Dictionary<string, HashSet<string>> dependencies) =>
        this.dependencies = dependencies;

    /// <summary>Full type name to the full names of every type it references.</summary>
    internal IReadOnlyDictionary<string, HashSet<string>> ByType => dependencies;

    internal static AssemblyDependencies Of(System.Reflection.Assembly assembly)
    {
        var path = assembly.Location;
        Assert.True(
            !string.IsNullOrEmpty(path) && File.Exists(path),
            $"The assembly '{assembly.GetName().Name}' has no readable file on disk, so its IL "
            + "cannot be inspected.");

        using var module = ModuleDefinition.ReadModule(path);
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var type in module.GetTypes())
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            Collect(type, used);
            used.Remove(Name(type));
            result[Name(type)] = used;
        }

        return new AssemblyDependencies(result);
    }

    /// <summary>
    /// The types in <paramref name="namespacePrefix"/> that use something in
    /// <paramref name="forbiddenPrefix"/> and are not excused by <paramref name="allowedPrefix"/>.
    /// </summary>
    internal IReadOnlyList<string> Offenders(
        string namespacePrefix,
        string forbiddenPrefix,
        string? allowedPrefix = null)
    {
        var offenders = new List<string>();

        foreach (var (type, used) in dependencies)
        {
            if (!InNamespace(type, namespacePrefix))
            {
                continue;
            }

            var breaches = used
                .Where(dependency => InNamespace(dependency, forbiddenPrefix))
                .Where(dependency => allowedPrefix is null || !InNamespace(dependency, allowedPrefix))
                .ToArray();

            if (breaches.Length > 0)
            {
                offenders.Add($"{type} -> {string.Join(", ", breaches.Order(StringComparer.Ordinal))}");
            }
        }

        return [.. offenders.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// The methods in this assembly that call any of <paramref name="methods"/>, each given as
    /// <c>Namespace.Type::Member</c> — for example <c>System.DateTime::get_UtcNow</c>.
    /// </summary>
    internal static IReadOnlyList<string> CallersOf(
        System.Reflection.Assembly assembly,
        params string[] methods)
    {
        var wanted = new HashSet<string>(methods, StringComparer.Ordinal);
        using var module = ModuleDefinition.ReadModule(assembly.Location);
        var callers = new List<string>();

        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                {
                    continue;
                }

                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is not MethodReference called)
                    {
                        continue;
                    }

                    var name = $"{called.DeclaringType.FullName}::{called.Name}";
                    if (wanted.Contains(name))
                    {
                        callers.Add($"{type.FullName}.{method.Name} calls {name}");
                    }
                }
            }
        }

        return [.. callers.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    internal IReadOnlyList<string> TypesIn(string namespacePrefix) =>
        [.. dependencies.Keys.Where(type => InNamespace(type, namespacePrefix)).Order(StringComparer.Ordinal)];

    /// <summary>
    /// True when <paramref name="typeName"/> is in <paramref name="prefix"/> or a namespace below
    /// it. Compared segment by segment so <c>Booking</c> never matches <c>BookingLegacy</c>.
    /// </summary>
    private static bool InNamespace(string typeName, string prefix) =>
        typeName.Equals(prefix, StringComparison.Ordinal)
        || typeName.StartsWith(prefix + ".", StringComparison.Ordinal);

    private static string Name(TypeReference type) =>
        string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";

    private static void Collect(TypeDefinition type, HashSet<string> used)
    {
        Add(type.BaseType, used);

        foreach (var @interface in type.Interfaces)
        {
            Add(@interface.InterfaceType, used);
        }

        AddAttributes(type.CustomAttributes, used);

        foreach (var field in type.Fields)
        {
            Add(field.FieldType, used);
            AddAttributes(field.CustomAttributes, used);
        }

        foreach (var property in type.Properties)
        {
            Add(property.PropertyType, used);
            AddAttributes(property.CustomAttributes, used);
        }

        foreach (var method in type.Methods)
        {
            Add(method.ReturnType, used);
            AddAttributes(method.CustomAttributes, used);

            foreach (var parameter in method.Parameters)
            {
                Add(parameter.ParameterType, used);
            }

            if (!method.HasBody)
            {
                continue;
            }

            foreach (var variable in method.Body.Variables)
            {
                Add(variable.VariableType, used);
            }

            foreach (var instruction in method.Body.Instructions)
            {
                AddOperand(instruction, used);
            }
        }
    }

    private static void AddOperand(Instruction instruction, HashSet<string> used)
    {
        switch (instruction.Operand)
        {
            case TypeReference type:
                Add(type, used);
                break;
            case MethodReference method:
                Add(method.DeclaringType, used);
                Add(method.ReturnType, used);
                foreach (var parameter in method.Parameters)
                {
                    Add(parameter.ParameterType, used);
                }

                break;
            case FieldReference field:
                Add(field.DeclaringType, used);
                Add(field.FieldType, used);
                break;
            default:
                break;
        }
    }

    private static void AddAttributes(IEnumerable<CustomAttribute> attributes, HashSet<string> used)
    {
        foreach (var attribute in attributes)
        {
            Add(attribute.AttributeType, used);
        }
    }

    private static void Add(TypeReference? type, HashSet<string> used)
    {
        if (type is null)
        {
            return;
        }

        // Unwrap arrays, by-ref, pointers and generic instances down to the named types.
        switch (type)
        {
            case GenericInstanceType generic:
                Add(generic.ElementType, used);
                foreach (var argument in generic.GenericArguments)
                {
                    Add(argument, used);
                }

                return;
            case TypeSpecification specification:
                Add(specification.ElementType, used);
                return;
            case GenericParameter:
                return;
            default:
                used.Add(Name(type));
                return;
        }
    }
}
