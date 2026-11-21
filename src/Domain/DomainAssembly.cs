using System.Reflection;

namespace Domain;

/// <summary>
/// Anchor for assembly scanning and for the architecture tests, which need a handle on this
/// assembly without depending on any particular rule type inside it.
/// </summary>
public static class DomainAssembly
{
    public static Assembly Value => typeof(DomainAssembly).Assembly;
}
