
namespace BonniePDF.Fluent;

/// <summary>Fluent rotation of native child content.</summary>
public static class ContainerRotationExtensions
{
    extension(IContentContainer c)
    {
        /// <summary>Rotate clockwise about the centre while keeping the original flow box.</summary>
        public IContentContainer Rotate(float angle) => c.Element(new Rotate { Angle = angle });

        /// <summary>Turn clockwise 90 degrees and swap layout dimensions.</summary>
        public IContentContainer RotateClockwise(float? contentWidth = null) =>
            c.Element(new Rotate { Angle = 90, Mode = RotationMode.Constrained, ContentWidth = contentWidth });

        /// <summary>Turn counterclockwise 90 degrees and swap layout dimensions.</summary>
        public IContentContainer RotateCounterclockwise(float? contentWidth = null) =>
            c.Element(new Rotate { Angle = -90, Mode = RotationMode.Constrained, ContentWidth = contentWidth });
    }
}
