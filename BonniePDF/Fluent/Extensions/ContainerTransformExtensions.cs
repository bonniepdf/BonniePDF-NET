using BonniePDF.Elements.Layout;

namespace BonniePDF.Fluent.Extensions;

/// <summary>Fluent mirroring and translation of native child content.</summary>
public static class ContainerTransformExtensions
{
    /// <summary>Mirror child content left to right inside its original box.</summary>
    public static IContentContainer FlipHorizontal(this IContentContainer c) => c.Element(new Flip { Horizontal = true });
    /// <summary>Mirror child content top to bottom inside its original box.</summary>
    public static IContentContainer FlipVertical(this IContentContainer c) => c.Element(new Flip { Vertical = true });
    /// <summary>Move painted content right/down by signed point offsets without changing flow.</summary>
    public static IContentContainer Translate(this IContentContainer c, float x, float y) => c.Element(new Translate { X = x, Y = y });
    /// <summary>Move painted content horizontally by a signed point offset.</summary>
    public static IContentContainer TranslateX(this IContentContainer c, float x) => c.Translate(x, 0);
    /// <summary>Move painted content vertically by a signed point offset.</summary>
    public static IContentContainer TranslateY(this IContentContainer c, float y) => c.Translate(0, y);
}
