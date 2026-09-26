// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Runtime.InteropServices;
namespace BonniePDF.Rendering.Svg.Sink;

/// <summary>
/// A two-dimensional coordinate in sink user space.
/// </summary>
/// <param name="X">The horizontal coordinate.</param>
/// <param name="Y">The vertical coordinate.</param>
/// <remarks>
/// Deliberately not a copy of BonniePDF's <c>Point</c>: the parser and IR must
/// not carry BonniePDF types. The BonniePDF adapter converts at the boundary.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct SinkPoint(float X, float Y);

/// <summary>
/// A straight RGBA colour, each channel normalised to 0..1.
/// </summary>
/// <param name="R">Red, 0..1.</param>
/// <param name="G">Green, 0..1.</param>
/// <param name="B">Blue, 0..1.</param>
/// <param name="A">Alpha, 0..1; 1 is fully opaque.</param>
/// <remarks>
/// 0..1 rather than 0..255 because every SVG colour form the parser resolves
/// normalises to floats, and because it matches what PDF operators consume —
/// so neither side of the seam rounds through bytes. BonniePDF's own
/// <c>Color</c> additionally carries palette, hex and scRGB machinery a sink
/// has no use for.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct SinkColor(float R, float G, float B, float A);
