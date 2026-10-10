// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BonniePDF.Rendering.Svg.Fonts;

/// <summary>The kind of a <see cref="GlyphSegment"/>.</summary>
internal enum GlyphSegmentKind
{
    /// <summary>Start a new contour.</summary>
    Move,

    /// <summary>A straight segment.</summary>
    Line,

    /// <summary>A quadratic Bezier segment.</summary>
    Quad,

    /// <summary>Close the current contour.</summary>
    Close,
}

/// <summary>
/// One command of a glyph outline, in font units with y pointing up - the
/// TrueType convention. Quadratic, because TrueType outlines are.
/// </summary>
/// <param name="Kind">What this segment does.</param>
/// <param name="X">End point x.</param>
/// <param name="Y">End point y.</param>
/// <param name="CX">Control point x, for <see cref="GlyphSegmentKind.Quad"/>.</param>
/// <param name="CY">Control point y, for <see cref="GlyphSegmentKind.Quad"/>.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct GlyphSegment(
    GlyphSegmentKind Kind,
    double X = 0,
    double Y = 0,
    double CX = 0,
    double CY = 0);

/// <summary>
/// The font seam: what the engine needs from a font to lay out and outline text.
/// </summary>
/// <remarks>
/// <para>
/// Text is imported as outlines, never as live text, so the engine needs glyph
/// shapes and advances - and nothing else. It never loads or chooses a font on its
/// own. Callers supply fonts through an <see cref="IFontProvider"/>; at fold-in,
/// BonniePDF's registered fonts fill it, exactly as <c>IVectorSink</c> is filled by
/// its <c>ICanvas</c>.
/// </para>
/// <para>
/// Consumers adapt their shared font reader to this seam. The SVG engine owns
/// no font decoder or font cache.
/// </para>
/// </remarks>
internal interface IFontFace
{
    /// <summary>Gets the family name, for diagnostics only.</summary>
    string Family { get; }

    /// <summary>Gets font units per em: a glyph at font size s is scaled by s / UnitsPerEm.</summary>
    int UnitsPerEm { get; }

    /// <summary>The glyph for a Unicode code point, or 0 when the font has none.</summary>
    int GlyphIndex(int codePoint);

    /// <summary>Horizontal advance of a glyph, in font units.</summary>
    double AdvanceWidth(int glyph);

    /// <summary>
    /// The pen adjustment between two adjacent glyphs, in font units - negative to
    /// tighten. Zero for a face without kerning.
    /// </summary>
    double Kerning(int left, int right);

    /// <summary>The glyph's outline, in font units. Empty for a blank glyph such as a space.</summary>
    IReadOnlyList<GlyphSegment> Outline(int glyph);
}

/// <summary>Supplies fonts by family name.</summary>
/// <remarks>
/// <para>
/// Called with one name at a time - the engine walks a <c>font-family</c> list itself,
/// in order - and with the CSS generic names (<c>sans-serif</c>, <c>serif</c>,
/// <c>monospace</c>, <c>cursive</c>, <c>fantasy</c>, <c>system-ui</c>) as they appear,
/// so the provider decides what each generic means. Matching is the provider's own
/// and should be exact: the engine deliberately applies no heuristic of the
/// "Helvetica Neue is close to Arial" kind, because a guessed font is a silent
/// substitution.
/// </para>
/// <para>
/// A provider may return a face that only approximates the request - a regular face
/// for a bold request, say. The engine draws whatever it is given and does not
/// synthesise bold or italic.
/// </para>
/// </remarks>
internal interface IFontProvider
{
    /// <summary>A face for one family name, or null.</summary>
    IFontFace? Resolve(string family, int weight, bool italic);
}
