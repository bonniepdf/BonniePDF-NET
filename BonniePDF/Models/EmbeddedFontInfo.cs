using BonniePDF.Typography;

namespace BonniePDF.Models;

/// <summary>
/// Contains all information about an embedded TrueType font
/// </summary>
public class EmbeddedFontInfo
{
    /// <summary>
    /// The parsed TrueType font
    /// </summary>
    public required TtfFont Font { get; init; }

    /// <summary>
    /// Tracks which glyphs are used in the document
    /// </summary>
    public required GlyphUsageTracker Tracker { get; init; }

    /// <summary>
    /// 6-character random prefix for subset font name (e.g., "ABCDEF")
    /// Required by PDF spec for subset fonts
    /// </summary>
    public required string SubsetPrefix { get; init; }

    /// <summary>
    /// PDF resource name (e.g., "/F1", "/F2")
    /// </summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// Object ID of the Type0 font in the PDF (set during writing)
    /// </summary>
    public int Type0ObjectId { get; set; }

    /// <summary>
    /// Gets the full font name with subset prefix (e.g., "ABCDEF+ArialMT")
    /// </summary>
    public string SubsetFontName => $"{SubsetPrefix}+{Font.PostScriptName}";
}