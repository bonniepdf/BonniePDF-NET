using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using System.Collections.Generic;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for vertical stacks
/// </summary>
public static class VerticalStackExtensions
{
    /// <summary>
    /// Adds a child element to the vertical stack
    /// </summary>
    public static VerticalStack Item(this VerticalStack s, IElement element)
    {
        s.Add(element);
        return s;
    }

    /// <summary>
    /// Adds multiple child elements to the vertical stack
    /// </summary>
    public static VerticalStack Items(this VerticalStack s, IEnumerable<IElement> elements)
    {
        foreach (var element in elements)
            s.Add(element);
        return s;
    }

    /// <summary>
    /// Adds multiple child elements to the vertical stack
    /// </summary>
    public static VerticalStack Items(this VerticalStack s, params IElement[] elements)
    {
        foreach (var element in elements)
            s.Add(element);
        return s;
    }
}
