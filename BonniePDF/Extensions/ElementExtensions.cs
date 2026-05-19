using BonniePDF.Elements;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for building PDF elements with a clean API
/// </summary>
public static class ElementExtensions
{
    /// <summary>
    /// Returns the element as a Container, wrapping it if necessary
    /// </summary>
    public static Container AsContainer(this IElement e)
    {
        return e is Container c ? c : new Container { Child = e };
    }
}