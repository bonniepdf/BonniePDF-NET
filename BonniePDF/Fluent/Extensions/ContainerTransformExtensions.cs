
namespace BonniePDF.Fluent;

/// <summary>Fluent mirroring and translation of native child content.</summary>
public static class ContainerTransformExtensions
{
    extension(IContentContainer c)
    {
        /// <summary>Mirror child content left to right inside its original box.</summary>
        public IContentContainer FlipHorizontal() => c.Element(new Flip { Horizontal = true });

        /// <summary>Mirror child content top to bottom inside its original box.</summary>
        public IContentContainer FlipVertical() => c.Element(new Flip { Vertical = true });

        /// <summary>Move painted content right/down by signed point offsets without changing flow.</summary>
        public IContentContainer Translate(float x, float y) => c.Element(new Translate { X = x, Y = y });

        /// <summary>Move painted content horizontally by a signed point offset.</summary>
        public IContentContainer TranslateX(float x) => c.Translate(x, 0);

        /// <summary>Move painted content vertically by a signed point offset.</summary>
        public IContentContainer TranslateY(float y) => c.Translate(0, y);
    }
}
