using BonniePDF.Configuration;

namespace BonniePDF.Rendering;

/// <summary>
/// Stores dimension information for a rendered page
/// </summary>
public class RenderedPageDimensions
{
    public PdfSize Size { get; set; }
    public float Margin { get; set; }
}