using System.Collections.Generic;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Options for writing the PDF catalog dictionary.
/// Groups optional object IDs that may be referenced from the catalog.
/// </summary>
public class CatalogOptions
{
    /// <summary>
    /// The viewer preferences object ID (0 if none).
    /// </summary>
    public int ViewerPrefsId { get; set; }

    /// <summary>
    /// The outlines (bookmarks) root object ID (0 if none).
    /// </summary>
    public int OutlinesId { get; set; }

    /// <summary>
    /// The XMP metadata stream object ID (0 if none).
    /// </summary>
    public int XmpMetadataId { get; set; }

    /// <summary>
    /// Output intent object IDs for PDF/A compliance (null or empty if none).
    /// </summary>
    public IList<int> OutputIntentIds { get; set; }

    /// <summary>
    /// Whether PDF/A mode is enabled (adds /MarkInfo to catalog).
    /// </summary>
    public bool PdfAEnabled { get; set; }

    /// <summary>
    /// The named destinations dictionary object ID (0 if none).
    /// </summary>
    public int DestsId { get; set; }
}
