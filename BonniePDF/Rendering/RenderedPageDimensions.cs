
namespace BonniePDF;

/// <summary>
/// Stores dimension information for a rendered page
/// </summary>
internal class RenderedPageDimensions
{
    public PdfSize Size { get; set; }
    public Thickness Margins { get; set; }

    /// <summary>
    /// Per-page background colour (from <see cref="Page.PageColor"/>), carried
    /// through to the finalisation pass so continuation/overflow pages inherit
    /// the colour of the page that originated them. <c>null</c> falls back to
    /// the document-level <see cref="DocumentSettings.PageColor"/>.
    /// </summary>
    public Color? PageColor { get; set; }

    /// <summary>
    /// Per-page background element (from <see cref="Page.PageBackground"/>),
    /// carried through to the finalisation pass so continuation/overflow pages
    /// inherit it. <c>null</c> falls back to the document-level
    /// <see cref="DocumentSettings.PageBackground"/> template.
    /// </summary>
    public IElement? PageBackground { get; set; }

    /// <summary>
    /// Per-page foreground element (from <see cref="Page.PageForeground"/>),
    /// carried through to the finalisation pass. <c>null</c> falls back to the
    /// document-level <see cref="DocumentSettings.PageForeground"/> template.
    /// </summary>
    public IElement? PageForeground { get; set; }
}