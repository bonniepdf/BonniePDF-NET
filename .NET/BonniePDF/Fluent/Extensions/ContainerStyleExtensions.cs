using BonniePDF.Core;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Fluent.Extensions;

/// <summary>
/// Fluent chaining extension methods for styling containers.
/// Each method creates a Container element and returns it as IContentContainer,
/// enabling continued chaining: .Padding(10).Border(1).Background(Color.Red)
/// </summary>
public static class ContainerStyleExtensions
{
    // --- Padding ---

    /// <summary>Sets uniform padding on all sides.</summary>
    public static IContentContainer Padding(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(value) };
        return c.Element(container);
    }

    /// <summary>Sets horizontal and vertical padding.</summary>
    public static IContentContainer Padding(this IContentContainer c, float horizontal, float vertical)
    {
        var container = new Container { Padding = new Thickness(horizontal, vertical) };
        return c.Element(container);
    }

    /// <summary>Sets padding for each side individually.</summary>
    public static IContentContainer Padding(this IContentContainer c, float left, float top, float right,
        float bottom)
    {
        var container = new Container { Padding = new Thickness(left, top, right, bottom) };
        return c.Element(container);
    }

    /// <summary>Sets top padding.</summary>
    public static IContentContainer PaddingTop(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(0, value, 0, 0) };
        return c.Element(container);
    }

    /// <summary>Sets bottom padding.</summary>
    public static IContentContainer PaddingBottom(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(0, 0, 0, value) };
        return c.Element(container);
    }

    /// <summary>Sets left padding.</summary>
    public static IContentContainer PaddingLeft(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(value, 0, 0, 0) };
        return c.Element(container);
    }

    /// <summary>Sets right padding.</summary>
    public static IContentContainer PaddingRight(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(0, 0, value, 0) };
        return c.Element(container);
    }

    /// <summary>Sets horizontal padding (left and right).</summary>
    public static IContentContainer PaddingHorizontal(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(value, 0, value, 0) };
        return c.Element(container);
    }

    /// <summary>Sets vertical padding (top and bottom).</summary>
    public static IContentContainer PaddingVertical(this IContentContainer c, float value)
    {
        var container = new Container { Padding = new Thickness(0, value, 0, value) };
        return c.Element(container);
    }

    // --- Background ---

    /// <summary>Sets the background color.</summary>
    public static IContentContainer Background(this IContentContainer c, Color color)
    {
        var container = new Container { BackgroundColor = color };
        return c.Element(container);
    }

    // --- Border ---

    /// <summary>Sets a uniform border on all sides.</summary>
    public static IContentContainer Border(this IContentContainer c, float width)
    {
        var container = new Container
        {
            Border = new Core.Border(width),
            BorderColors = new BorderColors(Color.Black)
        };
        return c.Element(container);
    }

    /// <summary>Sets a uniform border with a specific color.</summary>
    public static IContentContainer Border(this IContentContainer c, float width, Color color)
    {
        var container = new Container
        {
            Border = new Core.Border(width),
            BorderColors = new BorderColors(color)
        };
        return c.Element(container);
    }

    /// <summary>Sets a bottom border.</summary>
    public static IContentContainer BorderBottom(this IContentContainer c, float width, Color? color = null)
    {
        var container = new Container
        {
            Border = Core.Border.BottomOnly(width),
            BorderColors = new BorderColors(color ?? Color.Black)
        };
        return c.Element(container);
    }

    /// <summary>Sets a top border.</summary>
    public static IContentContainer BorderTop(this IContentContainer c, float width, Color? color = null)
    {
        var container = new Container
        {
            Border = Core.Border.TopOnly(width),
            BorderColors = new BorderColors(color ?? Color.Black)
        };
        return c.Element(container);
    }

    /// <summary>Sets a left border.</summary>
    public static IContentContainer BorderLeft(this IContentContainer c, float width, Color? color = null)
    {
        var container = new Container
        {
            Border = Core.Border.LeftOnly(width),
            BorderColors = new BorderColors(color ?? Color.Black)
        };
        return c.Element(container);
    }

    /// <summary>Sets a right border.</summary>
    public static IContentContainer BorderRight(this IContentContainer c, float width, Color? color = null)
    {
        var container = new Container
        {
            Border = Core.Border.RightOnly(width),
            BorderColors = new BorderColors(color ?? Color.Black)
        };
        return c.Element(container);
    }

    // --- Size Constraints ---

    /// <summary>Sets a fixed width.</summary>
    public static IContentContainer Width(this IContentContainer c, float value)
    {
        var container = new Container { Width = value };
        return c.Element(container);
    }

    /// <summary>Sets a fixed height.</summary>
    public static IContentContainer Height(this IContentContainer c, float value)
    {
        var container = new Container { Height = value };
        return c.Element(container);
    }

    // --- Corner Radius ---

    /// <summary>Sets the corner radius for rounded corners.</summary>
    public static IContentContainer CornerRadius(this IContentContainer c, float radius)
    {
        var container = new Container { CornerRadius = radius };
        return c.Element(container);
    }

    // --- Margin ---

    /// <summary>Sets uniform margin on all sides.</summary>
    public static IContentContainer Margin(this IContentContainer c, float value)
    {
        var container = new Container { Margin = new Core.Thickness(value) };
        return c.Element(container);
    }

    /// <summary>Sets horizontal and vertical margin.</summary>
    public static IContentContainer Margin(this IContentContainer c, float horizontal, float vertical)
    {
        var container = new Container { Margin = new Core.Thickness(horizontal, vertical) };
        return c.Element(container);
    }

    /// <summary>Sets margin for each side individually.</summary>
    public static IContentContainer Margin(this IContentContainer c, float left, float top, float right, float bottom)
    {
        var container = new Container { Margin = new Core.Thickness(left, top, right, bottom) };
        return c.Element(container);
    }

    // --- Alignment ---

    /// <summary>Centers the content horizontally.</summary>
    public static IContentContainer AlignCenter(this IContentContainer c)
    {
        // Alignment is achieved via a container with centered text blocks
        // For now, we pass through — alignment is handled at the text level
        var container = new Container();
        return c.Element(container);
    }

    /// <summary>Aligns content to the right.</summary>
    public static IContentContainer AlignRight(this IContentContainer c)
    {
        var container = new Container();
        return c.Element(container);
    }

    /// <summary>Aligns content to the left (default).</summary>
    public static IContentContainer AlignLeft(this IContentContainer c)
    {
        var container = new Container();
        return c.Element(container);
    }
}
