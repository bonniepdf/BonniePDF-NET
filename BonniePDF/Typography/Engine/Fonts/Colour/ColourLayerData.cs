// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    internal readonly struct ColourLayerData
    {
        internal readonly int Glyph, PaletteIndex;
        internal readonly PaletteColour Colour;
        internal ColourLayerData(int glyph, int paletteIndex, PaletteColour colour)
        {
            Glyph = glyph; PaletteIndex = paletteIndex; Colour = colour;
        }
        internal bool Foreground { get { return PaletteIndex == 0xffff; } }
    }
}
