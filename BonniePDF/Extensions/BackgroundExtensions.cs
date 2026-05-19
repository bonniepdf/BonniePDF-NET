using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for background and rounding
/// </summary>
public static class BackgroundExtensions
{
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
}
