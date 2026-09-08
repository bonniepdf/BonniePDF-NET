using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;

namespace BonniePDF.Rendering;

/// <summary>
/// Data emitted for a single rendered page
/// </summary>
public class PageRenderOutput
{
    public required string ContentStream { get; set; }
    public PdfSize Size { get; set; }
    public required IList<Hyperlink> Hyperlinks { get; set; }

    /// <summary>
    /// The page's resolved solid fill (a per-page PageColor overriding the document
    /// default), when one is set. Recorded for the layout snapshot so the contrast
    /// preflight can resolve a backdrop for text with no coloured ancestor.
    /// </summary>
    public Color? PageColor { get; set; }

    /// <summary>
    /// True when this page paints a background <em>element</em> rather than (or as
    /// well as) a flat colour, so its backdrop cannot be reduced to one opaque
    /// colour.
    /// </summary>
    public bool PageBackdropUnknown { get; set; }
}