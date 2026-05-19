using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;

namespace BonniePDF.Rendering;

/// <summary>
/// Data emitted for a single rendered page
/// </summary>
public class PageRenderOutput
{
    public string ContentStream { get; set; }
    public PdfSize Size { get; set; }
    public IList<Hyperlink> Hyperlinks { get; set; }
}