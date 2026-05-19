using BonniePDF.Elements;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for sizing elements
/// </summary>
public static class SizeExtensions
{
    /// <summary>
    /// Sets a fixed width for the element
    /// </summary>
    public static IElement Width(this IElement e, float value)
    {
        Container c = e.AsContainer();
        c.Width = value;
        return c;
    }

    /// <summary>
    /// Sets a fixed height for the element
    /// </summary>
    public static IElement Height(this IElement e, float value)
    {
        Container c = e.AsContainer();
        c.Height = value;
        return c;
    }

    /// <summary>
    /// Sets a fixed size (width and height) for the element
    /// </summary>
    public static IElement Size(this IElement e, float width, float height)
    {
        Container c = e.AsContainer();
        c.Width = width;
        c.Height = height;
        return c;
    }
}
