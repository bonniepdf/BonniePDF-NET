
namespace BonniePDF.Drawing;

/// <summary>
/// Low-level rendering options threaded into <see cref="SvgCanvas"/> by the SVG
/// render pipeline. This is the internal counterpart of the public
/// <see cref="SvgPageExportOptions"/>; it carries only the knobs the canvas
/// itself needs.
/// </summary>
public sealed class SvgRenderOptions
{
    /// <summary>
    /// Decimal places used when formatting coordinates. When <c>null</c>,
    /// coordinates are emitted verbatim, preserving the byte-for-byte output of
    /// the standalone chart export path.
    /// </summary>
    public int? Precision { get; init; }

    /// <summary>
    /// How raster images are represented. Defaults to <see cref="SvgImageEmbedding.DataUri"/>
    /// </summary>
    public SvgImageEmbedding EmbedImages { get; init; } = SvgImageEmbedding.DataUri;

    /// <summary>
    /// When <c>true</c>, text baselines are placed a full em below the top of the
    /// line box (<c>y + fontSize</c>), matching the PDF render pipeline exactly.
    /// When <c>false</c> (the default, used by the legacy chart path) the
    /// historical <c>y + fontSize*0.8</c> approximation is used.
    /// </summary>
    public bool PdfBaseline { get; init; }

    /// <summary>
    /// Shared default instance that reproduces the legacy canvas behaviour.
    /// </summary>
    public static readonly SvgRenderOptions Legacy = new();
}
