
namespace BonniePDF;

/// <summary>
/// Stores dimension information for a page
/// </summary>
public class PageDimensions
{
    /// <summary>The page size.</summary>
    public PdfSize Size { get; set; }
    /// <summary>The page margin, in points.</summary>
    public float Margin { get; set; }
}