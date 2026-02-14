using System.Collections.Generic;
using BonniePDF.Models;

namespace BonniePDF.IO;

/// <summary>
/// Data emitted for a single rendered page
/// </summary>
public class PageRenderOutput
{
    public string ContentStream { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public List<Hyperlink> Hyperlinks { get; set; }
}