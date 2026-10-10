// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class BitmapGlyphData
    {
        private readonly byte[] png;
        internal readonly int Width, Height, BearingX, BearingY, Ppem;
        internal readonly int AdvanceX, VerticalBearingX, VerticalBearingY, AdvanceY, PpemX, PpemY, StrikeIndex, ImageFormat;
        internal readonly bool HasVerticalMetrics;

        internal BitmapGlyphData(byte[] png, int width, int height, int bearingX, int bearingY, int advanceX,
            int verticalBearingX, int verticalBearingY, int advanceY, bool hasVerticalMetrics, BitmapStrikeData strike, int imageFormat)
        {
            this.png = (byte[])png.Clone();
            Width = width;
            Height = height;
            BearingX = bearingX;
            BearingY = bearingY;
            AdvanceX = advanceX; VerticalBearingX = verticalBearingX; VerticalBearingY = verticalBearingY;
            AdvanceY = advanceY; HasVerticalMetrics = hasVerticalMetrics;
            Ppem = PpemY = strike.PpemY; PpemX = strike.PpemX; StrikeIndex = strike.Index; ImageFormat = imageFormat;
        }

        internal byte[] GetPngBytes() { return (byte[])png.Clone(); }
    }
}
