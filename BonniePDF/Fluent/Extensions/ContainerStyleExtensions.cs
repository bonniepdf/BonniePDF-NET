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
    private static Container GetOrCreateContainer(IContentContainer c)
    {
        if (c is Container existingContainer && existingContainer.Child == null)
        {
            return existingContainer;
        }

        var newContainer = new Container();
        c.Element(newContainer);
        return newContainer;
    }

    // --- Padding ---

    /// <summary>Sets uniform padding on all sides.</summary>
    public static IContentContainer Padding(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(value);
        return container;
    }

    /// <summary>Sets horizontal and vertical padding.</summary>
    public static IContentContainer Padding(this IContentContainer c, float horizontal, float vertical)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(horizontal, vertical);
        return container;
    }

    /// <summary>Sets padding for each side individually.</summary>
    public static IContentContainer Padding(this IContentContainer c, float left, float top, float right,
        float bottom)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(left, top, right, bottom);
        return container;
    }

    /// <summary>Sets top padding.</summary>
    public static IContentContainer PaddingTop(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(0, value, 0, 0);
        return container;
    }

    /// <summary>Sets bottom padding.</summary>
    public static IContentContainer PaddingBottom(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(0, 0, 0, value);
        return container;
    }

    /// <summary>Sets left padding.</summary>
    public static IContentContainer PaddingLeft(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(value, 0, 0, 0);
        return container;
    }

    /// <summary>Sets right padding.</summary>
    public static IContentContainer PaddingRight(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(0, 0, value, 0);
        return container;
    }

    /// <summary>Sets horizontal padding (left and right).</summary>
    public static IContentContainer PaddingHorizontal(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(value, 0, value, 0);
        return container;
    }

    /// <summary>Sets vertical padding (top and bottom).</summary>
    public static IContentContainer PaddingVertical(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Padding = new Thickness(0, value, 0, value);
        return container;
    }

    // --- Background ---

    /// <summary>Sets the background color.</summary>
    public static IContentContainer Background(this IContentContainer c, Color color)
    {
        var container = GetOrCreateContainer(c);
        container.BackgroundColor = color;
        return container;
    }

    // --- Border ---

    /// <summary>Sets a uniform border on all sides.</summary>
    public static IContentContainer Border(this IContentContainer c, float width)
    {
        var container = GetOrCreateContainer(c);
        container.Border = new Core.Border(width);
        container.BorderColors = new BorderColors(Color.Black);
        return container;
    }

    /// <summary>Sets a uniform border with a specific color.</summary>
    public static IContentContainer Border(this IContentContainer c, float width, Color color)
    {
        var container = GetOrCreateContainer(c);
        container.Border = new Core.Border(width);
        container.BorderColors = new BorderColors(color);
        return container;
    }

    /// <summary>Sets a bottom border.</summary>
    public static IContentContainer BorderBottom(this IContentContainer c, float width, Color? color = null)
    {
        var container = GetOrCreateContainer(c);
        container.Border = Core.Border.BottomOnly(width);
        container.BorderColors = new BorderColors(color ?? Color.Black);
        return container;
    }

    /// <summary>Sets a top border.</summary>
    public static IContentContainer BorderTop(this IContentContainer c, float width, Color? color = null)
    {
        var container = GetOrCreateContainer(c);
        container.Border = Core.Border.TopOnly(width);
        container.BorderColors = new BorderColors(color ?? Color.Black);
        return container;
    }

    /// <summary>Sets a left border.</summary>
    public static IContentContainer BorderLeft(this IContentContainer c, float width, Color? color = null)
    {
        var container = GetOrCreateContainer(c);
        container.Border = Core.Border.LeftOnly(width);
        container.BorderColors = new BorderColors(color ?? Color.Black);
        return container;
    }

    /// <summary>Sets a right border.</summary>
    public static IContentContainer BorderRight(this IContentContainer c, float width, Color? color = null)
    {
        var container = GetOrCreateContainer(c);
        container.Border = Core.Border.RightOnly(width);
        container.BorderColors = new BorderColors(color ?? Color.Black);
        return container;
    }

    /// <summary>Sets horizontal borders (left and right), matching the PaddingHorizontal convention.</summary>
    public static IContentContainer BorderHorizontal(this IContentContainer c, float width, Color? color = null)
    {
        var container = GetOrCreateContainer(c);
        container.Border = Core.Border.VerticalSides(width);
        container.BorderColors = new BorderColors(color ?? Color.Black);
        return container;
    }

    /// <summary>Sets vertical borders (top and bottom), matching the PaddingVertical convention.</summary>
    public static IContentContainer BorderVertical(this IContentContainer c, float width, Color? color = null)
    {
        var container = GetOrCreateContainer(c);
        container.Border = Core.Border.HorizontalSides(width);
        container.BorderColors = new BorderColors(color ?? Color.Black);
        return container;
    }

    // --- Size Constraints ---

    /// <summary>Sets a fixed width.</summary>
    public static IContentContainer Width(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Width = value;
        return container;
    }

    /// <summary>Sets a fixed height.</summary>
    public static IContentContainer Height(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Height = value;
        return container;
    }

    /// <summary>
    /// Takes the body content height left on the current page instead of sizing
    /// to content - the middle band of a header / body / footer page.
    /// </summary>
    /// <remarks>
    /// A fixed <see cref="Height(IContentContainer, float)"/> wins outright, and
    /// fill never shrinks content below its natural height. See
    /// <see cref="Container.FillHeight"/>.
    /// </remarks>
    public static IContentContainer FillHeight(this IContentContainer c)
    {
        var container = GetOrCreateContainer(c);
        container.FillHeight = true;
        return container;
    }

    /// <summary>Sets a fixed width and height.</summary>
    public static IContentContainer Size(this IContentContainer c, float width, float height)
    {
        var container = GetOrCreateContainer(c);
        container.Width = width;
        container.Height = height;
        return container;
    }

    // --- Corner Radius ---

    /// <summary>Sets the corner radius for rounded corners.</summary>
    public static IContentContainer CornerRadius(this IContentContainer c, float radius)
    {
        var container = GetOrCreateContainer(c);
        container.CornerRadius = radius;
        return container;
    }

    /// <summary>
    /// Rounds the container's corners <em>and</em> clips its content to that
    /// rounded outline. Unlike <see cref="CornerRadius"/> — which only
    /// rounds the background fill and border stroke — this masks the child too, so
    /// a full-bleed image or any overflowing content is cut to the rounded box.
    /// This is the idiomatic way to build rounded image cards and avatars.
    /// <example>
    /// <code>
    /// container.RoundedCorners(12).Image("avatar.png");
    /// container.RoundedCorners(6).Background(ColorPalette.Slate[100]);
    /// </code>
    /// </example>
    /// </summary>
    public static IContentContainer RoundedCorners(this IContentContainer c, float radius)
    {
        var container = GetOrCreateContainer(c);
        container.CornerRadius = radius;
        container.ClipContent = true;
        return container;
    }

    /// <summary>
    /// Renders the container's subtree inside a crisp-edge region: on SVG
    /// export, thin axis-aligned strokes/rects (grid/table lines, dividers) snap to
    /// the pixel grid instead of being antialiased. No-op on PDF output. Apply only
    /// to axis-aligned content — rounded shapes, diagonals, and text want
    /// antialiasing.
    /// </summary>
    public static IContentContainer CrispEdges(this IContentContainer c)
    {
        var container = GetOrCreateContainer(c);
        container.CrispEdges = true;
        return container;
    }

    // --- Margin ---

    /// <summary>Sets uniform margin on all sides.</summary>
    public static IContentContainer Margin(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(value);
        return container;
    }

    /// <summary>Sets horizontal and vertical margin.</summary>
    public static IContentContainer Margin(this IContentContainer c, float horizontal, float vertical)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(horizontal, vertical);
        return container;
    }

    /// <summary>Sets margin for each side individually.</summary>
    public static IContentContainer Margin(this IContentContainer c, float left, float top, float right, float bottom)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(left, top, right, bottom);
        return container;
    }

    /// <summary>Sets top margin.</summary>
    public static IContentContainer MarginTop(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(0, value, 0, 0);
        return container;
    }

    /// <summary>Sets bottom margin.</summary>
    public static IContentContainer MarginBottom(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(0, 0, 0, value);
        return container;
    }

    /// <summary>Sets left margin.</summary>
    public static IContentContainer MarginLeft(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(value, 0, 0, 0);
        return container;
    }

    /// <summary>Sets right margin.</summary>
    public static IContentContainer MarginRight(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(0, 0, value, 0);
        return container;
    }

    /// <summary>Sets horizontal margin (left and right).</summary>
    public static IContentContainer MarginHorizontal(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(value, 0, value, 0);
        return container;
    }

    /// <summary>Sets vertical margin (top and bottom).</summary>
    public static IContentContainer MarginVertical(this IContentContainer c, float value)
    {
        var container = GetOrCreateContainer(c);
        container.Margin = new Core.Thickness(0, value, 0, value);
        return container;
    }

    // --- Alignment ---

    /// <summary>Centers the content horizontally.</summary>
    public static IContentContainer AlignCenter(this IContentContainer c)
    {
        var container = GetOrCreateContainer(c);
        container.HorizontalAlignment = HorizontalAlignment.Center;
        return container;
    }

    /// <summary>Aligns content to the right.</summary>
    public static IContentContainer AlignRight(this IContentContainer c)
    {
        var container = GetOrCreateContainer(c);
        container.HorizontalAlignment = HorizontalAlignment.End;
        return container;
    }

    /// <summary>Aligns content to the left (default).</summary>
    public static IContentContainer AlignLeft(this IContentContainer c)
    {
        var container = GetOrCreateContainer(c);
        container.HorizontalAlignment = HorizontalAlignment.Start;
        return container;
    }
}
