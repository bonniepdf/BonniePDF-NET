
namespace BonniePDF.Fluent;

/// <summary>Fluent rotation of native child content.</summary>
public static class ContainerRotationExtensions
{
    /// <summary>Rotate clockwise about the centre while keeping the original flow box.</summary>
    public static IContentContainer Rotate(this IContentContainer c, float angle) => c.Element(new Rotate { Angle = angle });
    /// <summary>Turn clockwise 90 degrees and swap layout dimensions.</summary>
    public static IContentContainer RotateClockwise(this IContentContainer c, float? contentWidth = null) =>
        c.Element(new Rotate { Angle = 90, Mode = RotationMode.Constrained, ContentWidth = contentWidth });
    /// <summary>Turn counterclockwise 90 degrees and swap layout dimensions.</summary>
    public static IContentContainer RotateCounterclockwise(this IContentContainer c, float? contentWidth = null) =>
        c.Element(new Rotate { Angle = -90, Mode = RotationMode.Constrained, ContentWidth = contentWidth });
}
