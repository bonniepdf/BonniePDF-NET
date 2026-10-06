
namespace BonniePDF;

/// <summary>
/// Fluent extension methods for padding
/// </summary>
public static class PaddingExtensions
{
    /// <summary>
    /// Adds uniform padding to all sides of the element
    /// </summary>
    public static IElement Padding(this IElement e, float uniform)
    {
        Container c = e.AsContainer();
        c.Padding = new Thickness(uniform);
        return c;
    }

    /// <summary>
    /// Adds horizontal and vertical padding to the element
    /// </summary>
    public static IElement Padding(this IElement e, float horizontal, float vertical)
    {
        Container c = e.AsContainer();
        c.Padding = new Thickness(horizontal, vertical);
        return c;
    }

    /// <summary>
    /// Adds individual padding values for each side of the element
    /// </summary>
    public static IElement Padding(this IElement e, float left, float top, float right, float bottom)
    {
        Container c = e.AsContainer();
        c.Padding = new Thickness(left, top, right, bottom);
        return c;
    }

    /// <summary>
    /// Adds left padding to the element
    /// </summary>
    public static IElement PaddingLeft(this IElement e, float value)
    {
        Container c = e.AsContainer();
        var p = c.Padding;
        c.Padding = new Thickness(value, p.Top, p.Right, p.Bottom);
        return c;
    }

    /// <summary>
    /// Adds right padding to the element
    /// </summary>
    public static IElement PaddingRight(this IElement e, float value)
    {
        Container c = e.AsContainer();
        var p = c.Padding;
        c.Padding = new Thickness(p.Left, p.Top, value, p.Bottom);
        return c;
    }

    /// <summary>
    /// Adds top padding to the element
    /// </summary>
    public static IElement PaddingTop(this IElement e, float value)
    {
        Container c = e.AsContainer();
        var p = c.Padding;
        c.Padding = new Thickness(p.Left, value, p.Right, p.Bottom);
        return c;
    }

    /// <summary>
    /// Adds bottom padding to the element
    /// </summary>
    public static IElement PaddingBottom(this IElement e, float value)
    {
        Container c = e.AsContainer();
        var p = c.Padding;
        c.Padding = new Thickness(p.Left, p.Top, p.Right, value);
        return c;
    }

    /// <summary>
    /// Adds horizontal padding to the element
    /// </summary>
    public static IElement PaddingHorizontal(this IElement e, float value)
    {
        Container c = e.AsContainer();
        var p = c.Padding;
        c.Padding = new Thickness(value, p.Top, value, p.Bottom);
        return c;
    }

    /// <summary>
    /// Adds vertical padding to the element
    /// </summary>
    public static IElement PaddingVertical(this IElement e, float value)
    {
        Container c = e.AsContainer();
        var p = c.Padding;
        c.Padding = new Thickness(p.Left, value, p.Right, value);
        return c;
    }
}
