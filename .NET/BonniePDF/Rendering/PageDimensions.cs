using BonniePDF.Models;

namespace BonniePDF.IO;

/// <summary>
/// Stores dimension information for a page
/// </summary>
public class PageDimensions
{
    public PdfSize Size { get; set; }
    public float Margin { get; set; }
}