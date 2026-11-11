namespace BonniePDF;

/// <summary>
/// Thrown when an element contains itself, directly or through its descendants. Such a tree has no end, so it is
/// rejected before layout instead of recursing until the stack overflows, which in .NET would end the process.
/// </summary>
public sealed class ElementCycleException : BonniePdfException
{
    /// <summary>Creates the exception, naming the type of the element found inside itself.</summary>
    public ElementCycleException(string elementType)
        : base($"The element tree contains a cycle: a {elementType} is its own descendant.")
    {
    }
}
