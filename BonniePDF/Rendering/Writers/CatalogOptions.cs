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

    /// <summary>
    /// The structure tree root object ID (0 if none). Set when the
    /// document opts into PDF/UA tagging.
    /// </summary>
    public int StructTreeRootId { get; set; }

    /// <summary>
    /// Whether the document carries a structure tree and should claim
    /// to be tagged in the catalog. When true the catalog emits
    /// <c>/MarkInfo &lt;&lt; /Marked true &gt;&gt;</c> regardless of
    /// <see cref="PdfAEnabled"/> (the two flags are orthogonal).
    /// </summary>
    public bool Tagged { get; set; }

    /// <summary>
    /// BCP-47 language tag for the document (e.g. <c>"en-GB"</c>).
    /// When non-null/empty, emitted as <c>/Lang</c> on the catalog.
    /// Required by PDF/UA-1.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// The /PageLabels number-tree object ID (0 if none).
    /// When non-zero, the catalog emits <c>/PageLabels N 0 R</c>.
    /// </summary>
    public int PageLabelsId { get; set; }
}
