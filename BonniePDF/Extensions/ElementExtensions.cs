using BonniePDF.Layout.Elements;
using BonniePDF.Models;

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

    /// <summary>
    /// Adds uniform padding to all sides of the element
    /// </summary>
    public static IElement Padding(this IElement e, float u)
    {
        Container c = e.AsContainer();
        c.Padding = new Thickness(u);
        return c;
    }

    /// <summary>
    /// Adds horizontal and vertical padding to the element
    /// </summary>
    public static IElement Padding(this IElement e, float h, float v)
    {
        Container c = e.AsContainer();
        c.Padding = new Thickness(h, v);
        return c;
    }

    /// <summary>
    /// Adds individual padding values for each side of the element
    /// </summary>
    public static IElement Padding(this IElement e, float l, float t, float r, float b)
    {
        Container c = e.AsContainer();
        c.Padding = new Thickness(l, t, r, b);
        return c;
    }

    /// <summary>
    /// Sets the background color of the element
    /// </summary>
    public static IElement Background(this IElement e, Color c)
    {
        Container k = e.AsContainer();
        k.BackgroundColor = c;
        return k;
    }

    /// <summary>
    /// Adds a border to the element with the specified color and width
    /// </summary>
    public static IElement Border(this IElement e, Color c, float w = 1)
    {
        Container k = e.AsContainer();
        k.Border = new Border(w);
        k.BorderColors = new BorderColors(c);
        return k;
    }

    /// <summary>
    /// Sets the text alignment to left
    /// </summary>
    public static TextBlock AlignLeft(this TextBlock b)
    {
        b.Alignment = TextAlignment.Left;
        return b;
    }

    /// <summary>
    /// Sets the text alignment to center
    /// </summary>
    public static TextBlock AlignCenter(this TextBlock b)
    {
        b.Alignment = TextAlignment.Center;
        return b;
    }

    /// <summary>
    /// Sets the text alignment to right
    /// </summary>
    public static TextBlock AlignRight(this TextBlock b)
    {
        b.Alignment = TextAlignment.Right;
        return b;
    }

    /// <summary>
    /// Sets the text alignment to justified
    /// </summary>
    public static TextBlock AlignJustify(this TextBlock b)
    {
        b.Alignment = TextAlignment.Justify;
        return b;
    }

    /// <summary>
    /// Adds underline decoration to the text block
    /// </summary>
    public static TextBlock Underline(this TextBlock b)
    {
        b.TextDecoration |= TextDecoration.Underline;
        return b;
    }

    /// <summary>
    /// Adds strikethrough decoration to the text block
    /// </summary>
    public static TextBlock Strikethrough(this TextBlock b)
    {
        b.TextDecoration |= TextDecoration.Strikethrough;
        return b;
    }

    /// <summary>
    /// Sets the corner radius for rounded corners, wrapping in a Container if needed
    /// </summary>
    public static IElement Rounded(this IElement e, float radius)
    {
        Container c = e.AsContainer();
        c.CornerRadius = radius;
        return c;
    }

    /// <summary>
    /// Sets the corner radius for rounded corners on a bordered element
    /// </summary>
    public static T Rounded<T>(this T e, float radius) where T : BorderedElementBase
    {
        e.CornerRadius = radius;
        return e;
    }

    /// <summary>
    /// Sets the font for the text block
    /// </summary>
    public static TextBlock Font(this TextBlock b, string fontName)
    {
        b.FontName = fontName;
        return b;
    }

    // ========== TableCell Extensions ==========

    /// <summary>
    /// Converts an IElement to a TableCell
    /// </summary>
    public static TableCell AsCell(this IElement e)
    {
        return new TableCell { Content = e };
    }

    /// <summary>
    /// Sets the background color for a TableCell
    /// </summary>
    public static TableCell Background(this TableCell c, Color color)
    {
        c.BackgroundColor = color;
        return c;
    }

    /// <summary>
    /// Sets the text color for a TableCell
    /// </summary>
    public static TableCell TextColor(this TableCell c, Color color)
    {
        c.TextColor = color;
        return c;
    }

    /// <summary>
    /// Sets the padding for a TableCell
    /// </summary>
    public static TableCell Padding(this TableCell c, float padding)
    {
        c.Padding = new Thickness(padding);
        return c;
    }

    /// <summary>
    /// Sets the padding for a TableCell with individual values
    /// </summary>
    public static TableCell Padding(this TableCell c, float left, float top, float right, float bottom)
    {
        c.Padding = new Thickness(left, top, right, bottom);
        return c;
    }

    /// <summary>
    /// Sets the border for a TableCell
    /// </summary>
    public static TableCell Border(this TableCell c, Color color, float width = 1)
    {
        c.Border = new Border(width);
        c.BorderColors = new BorderColors(color);
        return c;
    }

    /// <summary>
    /// Sets the column span for a TableCell
    /// </summary>
    public static TableCell ColSpan(this TableCell c, int span)
    {
        c.ColumnSpan = span;
        return c;
    }

}