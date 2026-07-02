namespace BonniePDF.Export;

/// <summary>
/// How raster images are represented in exported SVG.
/// </summary>
public enum SvgImageEmbedding
{
    /// <summary>Embed the image bytes inline as a <c>data:</c> URI (default).</summary>
    DataUri,

    /// <summary>Reserved for an external <c>href</c> reference mode (treated as <see cref="DataUri"/> until implemented).</summary>
    ExternalRef,

    /// <summary>Omit images entirely.</summary>
    Omit
}

/// <summary>
/// How text is represented in exported SVG.
/// </summary>
public enum SvgTextMode
{
    /// <summary>Selectable, compact text positioned with the document's real font metrics (default).</summary>
    Text,

    /// <summary>Reserved for a future glyph-outline mode that removes any dependency on viewer-installed fonts.</summary>
    Paths
}

/// <summary>
/// Options controlling how a document page is exported to SVG.
///
/// Mirrors the public surface documented in api-gap this feature (faithful page SVG
/// export) and the TypeScript <c>SvgPageExportOptions</c>. All members have
/// sensible defaults that produce compact, deterministic, standalone SVG.
/// </summary>
public sealed class SvgPageExportOptions
{
    /// <summary>Decimal places for generated coordinates. Defaults to 3 for stable, compact output.</summary>
    public int Precision { get; init; } = 3;

    /// <summary>How raster images should be represented. Defaults to <see cref="SvgImageEmbedding.DataUri"/>.</summary>
    public SvgImageEmbedding EmbedImages { get; init; } = SvgImageEmbedding.DataUri;

    /// <summary>How text should be represented. Defaults to <see cref="SvgTextMode.Text"/>.</summary>
    public SvgTextMode TextMode { get; init; } = SvgTextMode.Text;

    /// <summary>Include SVG <c>&lt;a&gt;</c> wrappers for PDF hyperlinks where possible. Defaults to <c>false</c>.</summary>
    public bool IncludeLinks { get; init; }

    /// <summary>
    /// Include <c>&lt;title&gt;</c>, <c>role</c>, and <c>aria-label</c> on the root
    /// <c>&lt;svg&gt;</c>. This is a lightweight preview affordance, not PDF/UA
    /// structure export. Defaults to <c>true</c> (matches the TypeScript port).
    /// </summary>
    public bool IncludeAccessibilityMetadata { get; init; } = true;

    /// <summary>
    /// Human-readable document title used to build the <c>&lt;title&gt;</c>/<c>aria-label</c>
    /// when <see cref="IncludeAccessibilityMetadata"/> is set. Defaults to the
    /// document metadata title, then a generic label.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Maximum number of pages to emit. The full document is still laid out (so
    /// pagination/overflow stays correct), but only the first <see cref="MaxPages"/>
    /// pages are serialized. <c>null</c> exports every page. Useful for
    /// preview/thumbnail pipelines that only need the first few pages.
    /// </summary>
    public int? MaxPages { get; init; }

    /// <summary>
    /// Capture a <see cref="BonniePDF.Rendering.LayoutSnapshot"/> of the rendered
    /// element tree (types, parent relationships, and per-page rectangles) and
    /// return it on the export result. Off by default and zero-cost when off: with
    /// no recorder attached, the layout pass is byte-identical to normal output.
    /// Used by the previewer's structure view to highlight where each element landed.
    /// </summary>
    public bool CaptureLayout { get; init; }
}
