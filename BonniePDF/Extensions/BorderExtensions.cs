using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for borders
/// </summary>
public static class BorderExtensions
{
    /// <summary>
    /// Adds a border to the element with the specified color and uniform width
    /// </summary>
    public static IElement Border(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        c.Border = new Border(width);
        c.BorderColors = new BorderColors(color);
        return c;
    }

    /// <summary>
    /// Adds a left border to the element
    /// </summary>
    public static IElement BorderLeft(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        var b = c.Border;
        var bc = c.BorderColors;
        
        c.Border = new Border(width, b.Top, b.Right, b.Bottom);
        c.BorderColors = new BorderColors(color, bc.Top, bc.Right, bc.Bottom);
        return c;
    }

    /// <summary>
    /// Adds a right border to the element
    /// </summary>
    public static IElement BorderRight(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        var b = c.Border;
        var bc = c.BorderColors;
        
        c.Border = new Border(b.Left, b.Top, width, b.Bottom);
        c.BorderColors = new BorderColors(bc.Left, bc.Top, color, bc.Bottom);
        return c;
    }

    /// <summary>
    /// Adds a top border to the element
    /// </summary>
    public static IElement BorderTop(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        var b = c.Border;
        var bc = c.BorderColors;
        
        c.Border = new Border(b.Left, width, b.Right, b.Bottom);
        c.BorderColors = new BorderColors(bc.Left, color, bc.Right, bc.Bottom);
        return c;
    }

    /// <summary>
    /// Adds a bottom border to the element
    /// </summary>
    public static IElement BorderBottom(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        var b = c.Border;
        var bc = c.BorderColors;
        
        c.Border = new Border(b.Left, b.Top, b.Right, width);
        c.BorderColors = new BorderColors(bc.Left, bc.Top, bc.Right, color);
        return c;
    }

    /// <summary>
    /// Adds horizontal borders (left and right) to the element
    /// </summary>
    public static IElement BorderHorizontal(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        var b = c.Border;
        var bc = c.BorderColors;
        
        c.Border = new Border(width, b.Top, width, b.Bottom);
        c.BorderColors = new BorderColors(color, bc.Top, color, bc.Bottom);
        return c;
    }

    /// <summary>
    /// Adds vertical borders (top and bottom) to the element
    /// </summary>
    public static IElement BorderVertical(this IElement e, Color color, float width = 1)
    {
        Container c = e.AsContainer();
        var b = c.Border;
        var bc = c.BorderColors;
        
        c.Border = new Border(b.Left, width, b.Right, width);
        c.BorderColors = new BorderColors(bc.Left, color, bc.Right, color);
        return c;
    }
}
