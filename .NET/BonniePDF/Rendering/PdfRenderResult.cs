using System.Collections.Generic;
using BonniePDF.Configuration;

namespace BonniePDF.Rendering;

/// <summary>
/// Contains the results of the PDF rendering phase (Phase 1).
/// This data structure holds all the calculated content streams, font mappings,
/// image resources, and hyperlinks needed to write the PDF file.
/// </summary>
public class PdfRenderResult
{
    /// <summary>
    /// The rendered content streams for each page
    /// </summary>
    public List<string> PageContentStreams { get; set; } = new();

    /// <summary>
    /// Maps page indices to their dimensions (width, height, margin)
    /// </summary>
    public Dictionary<int, PageDimensions> PageDimensions { get; set; } = new();

    /// <summary>
    /// The render context containing font mappings, image mappings, and hyperlinks
    /// </summary>
    public RenderContext RenderContext { get; set; }

    /// <summary>
    /// The oriented page size from document settings
    /// </summary>
    public PdfSize OrientedPageSize { get; set; }
}