using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using BonniePDF.Typography;
using BonniePDF.Typography.Engine;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>
/// Provides font metrics for calculating text width in PDF documents.
/// Supports both standard PDF fonts and custom TTF fonts.
/// </summary>
/// <remarks>
/// Parsed-font caches and aliases explicitly registered by application code are
/// process-wide and thread-safe. Aliases discovered while rendering are scoped
/// to the current document's async flow so one document cannot change another
/// document's wrapping or pagination.
/// </remarks>
public static class FontMetrics
{
    // Cache for parsed TTF widths: "C:/Fonts/arial.ttf" -> int[95]
    private static readonly ConcurrentDictionary<string, int[]> TtfCache = new();

    // Cache for full TtfFont instances loaded by file path. Used to measure text
    // with accurate per-glyph advances (including supplementary-plane characters
    // such as emoji), instead of the ASCII-only widths in TtfCache.
    private static readonly ConcurrentDictionary<string, TtfFont?> TtfFontCache = new();

    // Marks file paths that failed to load as a full TtfFont, so we fall back to
    // the ASCII-only measurement path without re-attempting on every call.
    // ConcurrentDictionary used as a set (value is unused) - there is no
    // ConcurrentHashSet in the BCL.
    private static readonly ConcurrentDictionary<string, byte> TtfFontLoadFailed = new(StringComparer.OrdinalIgnoreCase);

    // Explicit aliases registered outside document generation remain process-wide
    // for backwards compatibility. Serializer-discovered aliases live in the
    // AsyncLocal scope below so one document cannot change another's layout.
    private static readonly ConcurrentDictionary<string, string> GlobalFontPathAliases = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, TtfFont> GlobalFontInstanceAliases = new(StringComparer.OrdinalIgnoreCase);
    private static readonly AsyncLocal<DocumentAliasScope?> CurrentDocumentScope = new();

    private sealed class DocumentAliasScope(bool embedStandardFonts)
    {
        public bool EmbedStandardFonts { get; } = embedStandardFonts;
        public DocumentFonts? DocumentFonts { get; set; }
        public Action<string>? ReportUnknownFamily { get; set; }
        public Dictionary<string, string> PathAliases { get; } = new(GlobalFontPathAliases, StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TtfFont> InstanceAliases { get; } = new(GlobalFontInstanceAliases, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class DocumentAliasScopeLease(DocumentAliasScope? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            CurrentDocumentScope.Value = previous;
            _disposed = true;
        }
    }

    /// <summary>
    /// Starts an async-flow-local font-alias scope for one document render.
    /// Embedded standard-font substitutes are resolved lazily on the first
    /// measurement, before layout makes a wrapping or pagination decision.
    /// </summary>
    internal static IDisposable BeginDocumentScope(bool embedStandardFonts, DocumentFonts? documentFonts = null,
        Action<string>? reportUnknownFamily = null)
    {
        DocumentAliasScope? previous = CurrentDocumentScope.Value;
        var scope = new DocumentAliasScope(embedStandardFonts) { ReportUnknownFamily = reportUnknownFamily };
        if (documentFonts is { IsEmpty: false })
        {
            scope.DocumentFonts = documentFonts;
            // Measurement runs before any resource is registered, so the faces' parsed fonts are
            // known to it from the start, under the same names the elements carry.
            foreach (DocumentFontFace face in documentFonts.Faces) scope.InstanceAliases[face.FontName] = face.Font;
        }
        CurrentDocumentScope.Value = scope;
        return new DocumentAliasScopeLease(previous);
    }


    /// <summary>Reports, to the render pass in progress, a font family that resolved to nothing and was drawn in Helvetica.</summary>
    internal static void ReportUnknownFamily(string family) => CurrentDocumentScope.Value?.ReportUnknownFamily?.Invoke(family);

    /// <summary>Whether a document is being laid out or rendered, so a font family is resolved now rather than when it was set.</summary>
    internal static bool IsGeneratingDocument => CurrentDocumentScope.Value != null;

    /// <summary>The document fonts of the render pass in progress, if that document has any.</summary>
    internal static DocumentFonts? ActiveDocumentFonts => CurrentDocumentScope.Value?.DocumentFonts;

    /// <summary>Bytes of a document font by its font name, within the render pass in progress.</summary>
    internal static byte[]? GetDocumentFontBytes(string fontName) =>
        ActiveDocumentFonts?.FaceForFontName(fontName)?.Bytes;

    /// <summary>
    /// Registers a logical font name alias pointing to a specific TTF file path.
    /// Should be called by PdfResourceRegistry when PDF/A mode maps a standard font to an embedded TTF.
    /// Ensures <see cref="GetStringWidth"/> measures using actual TTF glyph metrics rather than
    /// the approximate standard-font width tables, so layout matches rendering.
    /// </summary>
    public static void RegisterFontAlias(string logicalName, string ttfPath)
    {
        DocumentAliasScope? scope = CurrentDocumentScope.Value;
        if (scope != null)
            scope.PathAliases[logicalName] = ttfPath;
        else
            GlobalFontPathAliases[logicalName] = ttfPath;
    }

    /// <summary>
    /// Registers a logical font name alias pointing to an already-parsed <see cref="TtfFont"/>.
    /// Used when the font was loaded from bytes (e.g., bundled Liberation Sans) rather than a
    /// file path, so file-based caching is bypassed entirely.
    /// </summary>
    internal static void RegisterFontInstance(string logicalName, TtfFont font)
    {
        DocumentAliasScope? scope = CurrentDocumentScope.Value;
        if (scope != null)
            scope.InstanceAliases[logicalName] = font;
        else
            GlobalFontInstanceAliases[logicalName] = font;
    }

    // -----------------------------------------------------------------------
    // RAW METRICS: Widths for ASCII 32 (' ') to 126 ('~') in 1/1000th of EM
    // -----------------------------------------------------------------------

    // ── Standard-14 advance-width tables ──────────────────────────────────────
    //
    // Source: ISO 32000-1 (PDF 1.7) Annex D, which tabulates the advance widths
    // of the 14 standard Type 1 fonts. These are factual font metrics reproduced
    // from the published international standard. A conformant PDF reader lays out
    // non-embedded standard fonts with these same values, so matching them keeps
    // our text measurement aligned with what the viewer actually renders.
    //
    // Each table is indexed by (codePoint - 32) over ASCII 32–126 (95 entries),
    // in the 1000-unit em used by PDF standard fonts.

    // Codes 0x80-0xFF (WinAnsiEncoding). Derived from the metric-compatible Liberation faces, which agree with
    // the tables above on every ASCII character; ISO 32000-1 Annex D does not tabulate these codes.
    // Each table: codes 0x80-0x9F then 0xA0-0xFF. 0 marks an undefined code.
    private static readonly int[] HelveticaHighWidths =
    {
        556, 0, 222, 556, 333, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0,
        0, 222, 222, 333, 333, 350, 556, 1000, 333, 1000, 500, 333, 944, 0, 500, 667,
        278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 552,
        400, 549, 333, 333, 333, 576, 537, 333, 333, 333, 365, 556, 834, 834, 834, 611,
        667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
        722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
        556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 549, 611, 556, 556, 556, 556, 500, 556, 500
    };

    private static readonly int[] HelveticaBoldHighWidths =
    {
        556, 0, 278, 556, 500, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0,
        0, 278, 278, 500, 500, 350, 556, 1000, 333, 1000, 556, 333, 944, 0, 500, 667,
        278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 552,
        400, 549, 333, 333, 333, 576, 556, 333, 333, 333, 365, 556, 834, 834, 834, 611,
        722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
        722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
        556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278,
        611, 611, 611, 611, 611, 611, 611, 549, 611, 611, 611, 611, 611, 556, 611, 556
    };

    private static readonly int[] TimesHighWidths =
    {
        500, 0, 333, 500, 444, 1000, 500, 500, 333, 1000, 556, 333, 889, 0, 611, 0,
        0, 333, 333, 444, 444, 350, 500, 1000, 333, 980, 389, 333, 722, 0, 444, 722,
        250, 333, 500, 500, 500, 500, 200, 500, 333, 760, 276, 500, 564, 333, 760, 500,
        400, 549, 300, 300, 333, 576, 453, 333, 333, 300, 310, 500, 750, 750, 750, 444,
        722, 722, 722, 722, 722, 722, 889, 667, 611, 611, 611, 611, 333, 333, 333, 333,
        722, 722, 722, 722, 722, 722, 722, 564, 722, 722, 722, 722, 722, 722, 556, 500,
        444, 444, 444, 444, 444, 444, 667, 444, 444, 444, 444, 444, 278, 278, 278, 278,
        500, 500, 500, 500, 500, 500, 500, 549, 500, 500, 500, 500, 500, 500, 500, 500
    };

    private static readonly int[] TimesBoldHighWidths =
    {
        500, 0, 333, 500, 500, 1000, 500, 500, 333, 1000, 556, 333, 1000, 0, 667, 0,
        0, 333, 333, 500, 500, 350, 500, 1000, 333, 1000, 389, 333, 722, 0, 444, 722,
        250, 333, 500, 500, 500, 500, 220, 500, 333, 747, 300, 500, 570, 333, 747, 500,
        400, 549, 300, 300, 333, 576, 540, 333, 333, 300, 330, 500, 750, 750, 750, 500,
        722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 389, 389, 389, 389,
        722, 722, 778, 778, 778, 778, 778, 570, 778, 722, 722, 722, 722, 722, 611, 556,
        500, 500, 500, 500, 500, 500, 722, 444, 444, 444, 444, 444, 278, 278, 278, 278,
        500, 556, 500, 500, 500, 500, 500, 549, 500, 556, 556, 556, 556, 500, 556, 500
    };

    private static readonly int[] TimesItalicHighWidths =
    {
        500, 0, 333, 500, 556, 889, 500, 500, 333, 1000, 500, 333, 944, 0, 556, 0,
        0, 333, 333, 556, 556, 350, 500, 889, 333, 980, 389, 333, 667, 0, 389, 556,
        250, 389, 500, 500, 500, 500, 275, 500, 333, 760, 276, 500, 675, 333, 760, 500,
        400, 549, 300, 300, 333, 576, 523, 250, 333, 300, 310, 500, 750, 750, 750, 500,
        611, 611, 611, 611, 611, 611, 889, 667, 611, 611, 611, 611, 333, 333, 333, 333,
        722, 667, 722, 722, 722, 722, 722, 675, 722, 722, 722, 722, 722, 556, 611, 500,
        500, 500, 500, 500, 500, 500, 667, 444, 444, 444, 444, 444, 278, 278, 278, 278,
        500, 500, 500, 500, 500, 500, 500, 549, 500, 500, 500, 500, 500, 444, 500, 444
    };

    private static readonly int[] TimesBoldItalicHighWidths =
    {
        500, 0, 333, 500, 500, 1000, 500, 500, 333, 1000, 556, 333, 944, 0, 611, 0,
        0, 333, 333, 500, 500, 350, 500, 1000, 333, 1000, 389, 333, 722, 0, 389, 611,
        250, 389, 500, 500, 500, 500, 220, 500, 333, 747, 266, 500, 606, 333, 747, 500,
        400, 549, 300, 300, 333, 576, 500, 250, 333, 300, 300, 500, 750, 750, 750, 500,
        667, 667, 667, 667, 667, 667, 944, 667, 667, 667, 667, 667, 389, 389, 389, 389,
        722, 722, 722, 722, 722, 722, 722, 570, 722, 722, 722, 722, 722, 611, 611, 500,
        500, 500, 500, 500, 500, 500, 722, 444, 444, 444, 444, 444, 278, 278, 278, 278,
        500, 556, 500, 500, 500, 500, 500, 549, 500, 556, 556, 556, 556, 444, 500, 444
    };

    // Helvetica Base
    private static readonly int[] HelveticaWidths =
    {
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584
    };

    // Helvetica-Bold
    private static readonly int[] HelveticaBoldWidths =
    {
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584
    };

    // Times-Roman Base
    private static readonly int[] TimesWidths =
    {
        250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444,
        921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722,
        556, 722, 667, 556, 611, 722, 722, 944, 722, 722, 611, 333, 278, 333, 469, 500,
        333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500,
        500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541
    };

    // Times-Bold
    private static readonly int[] TimesBoldWidths =
    {
        250, 333, 555, 500, 500, 1000, 833, 278, 333, 333, 500, 570, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 570, 570, 570, 500,
        930, 722, 667, 722, 722, 667, 611, 778, 778, 389, 500, 778, 667, 944, 722, 778,
        611, 778, 722, 556, 667, 722, 722, 1000, 722, 722, 667, 333, 278, 333, 581, 500,
        333, 500, 556, 444, 556, 444, 333, 500, 556, 278, 333, 556, 278, 833, 556, 500,
        556, 556, 444, 389, 333, 556, 500, 722, 500, 500, 444, 394, 220, 394, 520
    };

    // Times-Italic
    private static readonly int[] TimesItalicWidths =
    {
        250, 333, 420, 500, 500, 833, 778, 214, 333, 333, 500, 675, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 675, 675, 675, 500,
        920, 611, 611, 667, 722, 611, 611, 722, 722, 333, 444, 667, 556, 833, 667, 722,
        611, 722, 611, 500, 556, 722, 611, 833, 611, 556, 556, 389, 278, 389, 422, 500,
        333, 500, 500, 444, 500, 444, 278, 500, 500, 278, 278, 444, 278, 722, 500, 500,
        500, 500, 389, 389, 278, 500, 444, 667, 444, 444, 389, 400, 275, 400, 541
    };

    // Times-BoldItalic
    private static readonly int[] TimesBoldItalicWidths =
    {
        250, 389, 555, 500, 500, 833, 778, 278, 333, 333, 500, 570, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 570, 570, 570, 500,
        832, 667, 667, 667, 722, 667, 667, 722, 778, 389, 500, 667, 611, 889, 722, 722,
        611, 722, 667, 556, 611, 722, 667, 889, 667, 611, 611, 333, 278, 333, 570, 500,
        333, 500, 500, 444, 500, 444, 333, 500, 556, 278, 278, 500, 278, 778, 556, 500,
        500, 500, 389, 389, 278, 556, 444, 667, 500, 444, 389, 348, 220, 348, 570
    };

    // Symbol and ZapfDingbats use their own font-specific encodings, so unlike
    // the Latin tables above these span codes 32–254 (223 entries, indexed by
    // code - 32). Unencoded positions (the C1 control block 127–159, and code
    // 240) are 0. A non-embedded standard font is drawn with its built-in
    // encoding, so measuring by the same code keeps layout aligned with the
    // rendered glyph.

    // Symbol
    private static readonly int[] SymbolWidths =
    {
        250, 333, 713, 500, 549, 833, 778, 439, 333, 333, 500, 549, 250, 549, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 549, 549, 549, 444,
        549, 722, 667, 722, 612, 611, 763, 603, 722, 333, 631, 722, 686, 889, 722, 722,
        768, 741, 556, 592, 611, 690, 439, 768, 645, 795, 611, 333, 863, 333, 658, 500,
        500, 631, 549, 549, 494, 439, 521, 411, 603, 329, 603, 549, 549, 576, 521, 549,
        549, 521, 549, 603, 439, 576, 713, 686, 493, 686, 494, 480, 200, 480, 549, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        750, 620, 247, 549, 167, 713, 500, 753, 753, 753, 753, 1042, 987, 603, 987, 603,
        400, 549, 411, 549, 549, 713, 494, 460, 549, 549, 549, 549, 1000, 603, 1000, 658,
        823, 686, 795, 987, 768, 768, 823, 768, 768, 713, 713, 713, 713, 713, 713, 713,
        768, 713, 790, 790, 890, 823, 549, 250, 713, 603, 603, 1042, 987, 603, 987, 603,
        494, 329, 790, 790, 786, 713, 384, 384, 384, 384, 384, 384, 494, 494, 494, 494,
        0, 329, 274, 686, 686, 686, 384, 384, 384, 384, 384, 384, 494, 494, 494
    };

    // ZapfDingbats
    private static readonly int[] ZapfDingbatsWidths =
    {
        278, 974, 961, 974, 980, 719, 789, 790, 791, 690, 960, 939, 549, 855, 911, 933,
        911, 945, 974, 755, 846, 762, 761, 571, 677, 763, 760, 759, 754, 494, 552, 537,
        577, 692, 786, 788, 788, 790, 793, 794, 816, 823, 789, 841, 823, 833, 816, 831,
        923, 744, 723, 749, 790, 792, 695, 776, 768, 792, 759, 707, 708, 682, 701, 826,
        815, 789, 789, 707, 687, 696, 689, 786, 787, 713, 791, 785, 791, 873, 761, 762,
        762, 759, 759, 892, 892, 788, 784, 438, 138, 277, 415, 392, 392, 668, 668, 0,
        390, 390, 317, 317, 276, 276, 509, 509, 410, 410, 234, 234, 334, 334, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 732, 544, 544, 910, 667, 760, 760, 776, 595, 694, 626, 788, 788, 788, 788,
        788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788,
        788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788, 788,
        788, 788, 788, 788, 894, 838, 1016, 458, 748, 924, 748, 918, 927, 928, 928, 834,
        873, 828, 924, 924, 917, 930, 931, 463, 883, 836, 836, 867, 867, 696, 696, 874,
        0, 874, 760, 946, 771, 865, 771, 888, 967, 888, 831, 873, 927, 970, 918
    };

    // =====================================================
    // 1. PUBLIC API
    // =====================================================

    /// <summary>
    /// Gets the width of a text string in points
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <param name="fontIdentifier">Font name (e.g., "Helvetica") or TTF file path (e.g., "C:/Fonts/arial.ttf")</param>
    /// <param name="fontSize">The font size in points</param>
    /// <returns>The width of the text in points</returns>
    public static float GetStringWidth(string text, string fontIdentifier, float fontSize, IReadOnlyDictionary<string,int>? features = null)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        TtfFont? font = ResolveShapingFont(fontIdentifier, out string? unparsedPath);
        if (font != null) return GetKernedStringWidth(text, font, fontSize, features);
        if (unparsedPath != null) return GetUnparsedTtfWidth(text, unparsedPath, fontSize);

        // C. IS IT A STANDARD FONT? (approximate hardcoded tables)
        if (StandardFontCoverage.IsStandardTextFace(fontIdentifier) && !StandardFontCoverage.CoversAll(text))
            return GetStandardFallbackWidth(text, fontIdentifier, fontSize, features);
        return GetStandardFontWidth(text, fontIdentifier, fontSize);
    }

    /// <summary>
    /// A standard text face's text that falls back in part, measured as it is painted: the clusters the face shows
    /// from its width tables, the rest from the shaped advances of the fallback faces that draw them.
    /// </summary>
    private static float GetStandardFallbackWidth(string text, string fontName, float fontSize, IReadOnlyDictionary<string,int>? features)
    {
        List<ShaperFont> candidates = StandardFallbackCandidates(fontName);
        var covered = new System.Text.StringBuilder();
        double fallback = 0;
        foreach (StandardTextSegment segment in StandardFontCoverage.Split(text))
        {
            if (segment.Covered) { covered.Append(segment.Text); continue; }
            foreach (ShapedRun run in TextRunShaper.ShapeTextRuns(segment.Text, candidates).Runs)
            {
                TtfFont face = candidates[run.FontIndex].Font;
                fallback += SumAdvances(face.Shape(run.Text, features)) * fontSize / face.UnitsPerEm;
            }
        }
        return (float)(GetStandardFontWidth(covered.ToString(), fontName, fontSize) + fallback);
    }

    private static List<ShaperFont> StandardFallbackCandidates(string fontName)
    {
        var candidates = new List<ShaperFont>();
        foreach (FallbackFont fallback in StandardFontCoverage.FallbackChain(fontName))
            candidates.Add(new ShaperFont { FontIdentifier = fallback.Key, Font = fallback.Font });
        return candidates;
    }

    /// <summary>
    /// The font <see cref="GetStringWidth"/> measures <paramref name="fontIdentifier"/> with by
    /// shaping (through <see cref="GetKernedStringWidth"/>), or null when it uses a width table:
    /// a standard font, or a TrueType file that failed to parse (<paramref name="unparsedPath"/>).
    /// </summary>
    internal static TtfFont? ResolveShapingFont(string fontIdentifier, out string? unparsedPath)
    {
        unparsedPath = null;

        // A. IS IT A CUSTOM TTF FILE (direct path)?
        if (fontIdentifier.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
            fontIdentifier.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
            return LoadMeasuringTtf(fontIdentifier, out unparsedPath);

        // A family registered through FontManager resolves to a name for that registration.
        if (FontManager.GetRegisteredFont(fontIdentifier) is { } registered) return registered.Font;

        DocumentAliasScope? scope = CurrentDocumentScope.Value;
        if (scope != null)
        {
            if (scope.InstanceAliases.TryGetValue(fontIdentifier, out TtfFont? scopedInstance))
                return scopedInstance;

            if (scope.PathAliases.TryGetValue(fontIdentifier, out string? scopedPath))
                return LoadMeasuringTtf(scopedPath, out unparsedPath);

            // PDF/A and PDF/UA embed bundled substitutes for the standard 12
            // Latin Type-1 fonts. Resolve that exact face before the first layout
            // measurement, rather than discovering it while drawing text.
            if (scope.EmbedStandardFonts && FontManager.GetParsedStandardFont(fontIdentifier) is TtfFont embedded)
            {
                scope.InstanceAliases[fontIdentifier] = embedded;
                return embedded;
            }

            return null;
        }

        // Outside document generation, preserve explicitly registered global aliases.
        if (GlobalFontInstanceAliases.TryGetValue(fontIdentifier, out TtfFont? instanceFont))
            return instanceFont;

        if (GlobalFontPathAliases.TryGetValue(fontIdentifier, out string? aliasedPath))
            return LoadMeasuringTtf(aliasedPath, out unparsedPath);

        return null;
    }

    /// <summary>
    /// Measures the width of a string using actual per-glyph advances and kerning
    /// from an embedded TrueType font, including shaping, feature overrides and fallback.
    /// Returns 0 if font is null or text is empty.
    /// </summary>
    internal static float GetKernedStringWidth(string? text, TtfFont? font, float fontSize, IReadOnlyDictionary<string,int>? features = null)
    {
        if (string.IsNullOrEmpty(text) || font == null) return 0;

        // Accumulated in double, not float. Every glyph adds a scaled advance, so
        // a single-precision running total compounds its own rounding error along
        // the string. The measurement feeds layout, and layout coordinates are
        // emitted rounded to three decimals: a right-aligned run measured a
        // fraction of a thousandth short lands on the other side of that rounding
        // from the TypeScript implementation, which accumulates in double. The
        // font metrics are integers and the scale is an exact promotion, so this
        // makes the two agree. The result narrows to float to keep the layout
        // contract unchanged.
        if (TextRunShaper.PrimaryCovers(text, font))
            return (float)(SumAdvances(font.Shape(text, features)) * fontSize / font.UnitsPerEm);
        return GetFallbackKernedStringWidth(text, font, fontSize, features);
    }

    // Kept out of GetKernedStringWidth so the common path does not allocate the closure
    // this path needs.
    private static float GetFallbackKernedStringWidth(string text, TtfFont font, float fontSize, IReadOnlyDictionary<string,int>? features)
    {
        var candidates=new List<ShaperFont> { new() {FontIdentifier="primary",Font=font} };
        foreach(var fallback in (ActiveDocumentFonts?.FallbackFonts()??[]).Concat(FontManager.GetFallbackFonts()))
            if(!ReferenceEquals(fallback.Font,font))candidates.Add(new ShaperFont {FontIdentifier=fallback.Key,Font=fallback.Font});
        return (float)TextRunShaper.ShapeTextRuns(text,candidates).Runs.Sum(run=>{var face=candidates[run.FontIndex].Font;return SumAdvances(face.Shape(run.Text,features))*fontSize/face.UnitsPerEm;});
    }

    // The left-to-right double sum Enumerable.Sum computes. Advances are integers and the sum
    // stays far below 2^53, so the exact integer total converted once is the same value.
    private static double SumAdvances(GlyphRun glyphs) => glyphs.TotalAdvanceX();

    // =====================================================
    // 2. CUSTOM TTF LOGIC
    // =====================================================
    // The parsed font for a TrueType path, or null (with unparsedPath set) when it fails to parse.
    private static TtfFont? LoadMeasuringTtf(string filepath, out string? unparsedPath)
    {
        unparsedPath = null;
        // Prefer the full TtfFont path: it enumerates Unicode runes (handling
        // surrogate pairs correctly for emoji and other supplementary-plane
        // characters) and queries real per-glyph advances. Without this, the
        // ASCII-only widths table below sees each surrogate code unit as an
        // unknown char and assigns the 500-unit fallback, badly under-measuring
        // wide emoji glyphs - wrapping then fails to trigger and content runs
        // off the page.
        if (!TtfFontLoadFailed.ContainsKey(filepath))
        {
            // GetOrAdd (rather than TryGetValue-then-set) so two threads racing on
            // the same uncached path can't both win the write; a racing failure
            // still records itself in TtfFontLoadFailed exactly as before.
            TtfFont? fullFont = TtfFontCache.GetOrAdd(filepath, path =>
            {
                try
                {
                    return TtfFont.LoadFromFileCached(path);
                }
                catch
                {
                    TtfFontLoadFailed.TryAdd(path, 0);
                    return null;
                }
            });
            if (fullFont != null) return fullFont;
        }
        unparsedPath = filepath;
        return null;
    }

    private static float GetUnparsedTtfWidth(string text, string filepath, float fontSize)
    {
        // Legacy fallback: ASCII-only width table for fonts that fail to parse
        // as a full TtfFont. Kept for robustness; not exercised in the common case.
        int[] widths;
        try
        {
            widths = TtfCache.GetOrAdd(filepath, SimpleTtfReader.GetWidths);
        }
        catch
        {
            // Fallback if file not found or corrupted
            return text.Length * (fontSize * 0.5f);
        }
        float totalWidth = 0;

        foreach (char c in text)
        {
            int index = c - 32;
            // If char is valid ASCII (32-126), use table. Otherwise default to 500 (average width).
            if (index >= 0 && index < widths.Length)
                totalWidth += widths[index];
            else
                totalWidth += 500;
        }

        return totalWidth / 1000f * fontSize;
    }

    // =====================================================
    // 3. STANDARD FONT LOGIC (Hardcoded Tables)
    // =====================================================
    private static TtfFont? MetricFace(string identifier)
    {
        var scope=CurrentDocumentScope.Value;
        if(scope?.InstanceAliases.TryGetValue(identifier,out var scoped)==true)return scoped;
        if(GlobalFontInstanceAliases.TryGetValue(identifier,out var global))return global;
        if(FontManager.GetRegisteredFont(identifier) is { } registered)return registered.Font;
        if(scope?.EmbedStandardFonts==true && FontManager.GetParsedStandardFont(identifier) is TtfFont standard)return standard;
        if(scope?.PathAliases.TryGetValue(identifier,out var scopedPath)==true)identifier=scopedPath;
        else if(GlobalFontPathAliases.TryGetValue(identifier,out var globalPath))identifier=globalPath;
        if(identifier.EndsWith(".ttf",StringComparison.OrdinalIgnoreCase)||identifier.EndsWith(".otf",StringComparison.OrdinalIgnoreCase)) {try{return TtfFont.LoadFromFileCached(identifier);}catch(IOException){return null;}}
        return null;
    }
    internal static bool UsesEmbeddedMetrics(string identifier) => MetricFace(identifier) is not null;

    internal static int GetGlyphCount(string text,string identifier,IReadOnlyDictionary<string,int>? features=null)
    {
        var font=MetricFace(identifier);
        if(font is null)
        {
            if(StandardFontCoverage.IsStandardTextFace(identifier) && !StandardFontCoverage.CoversAll(text))
                return GetStandardFallbackGlyphCount(text, identifier, features);
            return text.EnumerateRunes().Count(c=>c.Value>=32);
        }
        if (TextRunShaper.PrimaryCovers(text, font)) return font.Shape(text, features).Count;
        var candidates=new List<ShaperFont>{new(){FontIdentifier="primary",Font=font}};
        foreach(var fallback in (ActiveDocumentFonts?.FallbackFonts()??[]).Concat(FontManager.GetFallbackFonts()))if(!ReferenceEquals(fallback.Font,font))candidates.Add(new ShaperFont {FontIdentifier=fallback.Key,Font=fallback.Font});
        return TextRunShaper.ShapeTextRuns(text,candidates).Runs.Sum(run=>candidates[run.FontIndex].Font.Shape(run.Text,features).Count);
    }
    private static int GetStandardFallbackGlyphCount(string text, string fontName, IReadOnlyDictionary<string,int>? features)
    {
        List<ShaperFont> candidates = StandardFallbackCandidates(fontName);
        int count = 0;
        foreach (StandardTextSegment segment in StandardFontCoverage.Split(text))
        {
            if (segment.Covered) { count += segment.Text.EnumerateRunes().Count(c => c.Value >= 32); continue; }
            foreach (ShapedRun run in TextRunShaper.ShapeTextRuns(segment.Text, candidates).Runs)
                count += candidates[run.FontIndex].Font.Shape(run.Text, features).Count;
        }
        return count;
    }

    internal static bool IsSafeTextBreak(string text,string identifier,int offset,IReadOnlyDictionary<string,int>? features=null)
    {
        return GetTextBreakPredicate(text, identifier, features)(offset);
    }

    internal static Func<int, bool> GetTextBreakPredicate(string text, string identifier, IReadOnlyDictionary<string,int>? features = null)
    {
        var glyphs = MetricFace(identifier)?.Shape(text, features);
        return offset =>
        {
            if (glyphs is null) return true;
            for (int i = 0; i < glyphs.Count; i++)
            {
                int cluster = glyphs.Cluster(i);
                if ((cluster < offset && offset < cluster + glyphs.SourceLength(i)) || (cluster == offset && glyphs.UnsafeToBreak(i))) return false;
            }
            return true;
        };
    }

    private static int[]? GetStandardFontHighTable(string fontName) => fontName switch
    {
        "Helvetica"             => HelveticaHighWidths,
        "Helvetica-Oblique"     => HelveticaHighWidths,
        "Helvetica-Bold"        => HelveticaBoldHighWidths,
        "Helvetica-BoldOblique" => HelveticaBoldHighWidths,
        "Times-Roman"           => TimesHighWidths,
        "Times-Bold"            => TimesBoldHighWidths,
        "Times-Italic"          => TimesItalicHighWidths,
        "Times-BoldItalic"      => TimesBoldItalicHighWidths,
        _                       => null,
    };

    private static int[]? GetStandardFontTable(string fontName) => fontName switch
    {
        "Helvetica"             => HelveticaWidths,
        "Helvetica-Oblique"     => HelveticaWidths,
        "Helvetica-Bold"        => HelveticaBoldWidths,
        "Helvetica-BoldOblique" => HelveticaBoldWidths,
        "Times-Roman"           => TimesWidths,
        "Times-Bold"            => TimesBoldWidths,
        "Times-Italic"          => TimesItalicWidths,
        "Times-BoldItalic"      => TimesBoldItalicWidths,
        "Symbol"                => SymbolWidths,
        "ZapfDingbats"          => ZapfDingbatsWidths,
        _                       => null,
    };

    private static float GetStandardFontWidth(string text, string fontName, float fontSize)
    {
        // Courier variants: all use a fixed 600-unit advance (monospace)
        if (fontName.Contains("Courier")) return text.EnumerateRunes().Count() * (fontSize * 0.6f);

        int[]? table = GetStandardFontTable(fontName);
        int[]? highTable = GetStandardFontHighTable(fontName);
        float totalWidth = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            // The text fonts show a character through its WinAnsi byte, so measure that byte.
            int code = highTable != null ? WinAnsi.Byte(rune.Value) : rune.Value;
            int w = code >= 128 && highTable != null ? highTable[code - 128]
                : table != null && code >= 32 && code - 32 < table.Length ? table[code - 32] : 0;
            totalWidth += w == 0 ? 500 : w;
        }
        return totalWidth / 1000f * fontSize;
    }

    // =====================================================
    // 4. TTF READER
    // =====================================================

    /// <summary>
    /// Simple TTF font reader that extracts character widths for ASCII characters
    /// </summary>
    internal class SimpleTtfReader
    {
        // We only care about ASCII 32-126 for this simple engine.
        // Returns an array where index 0 is space (32), index 1 is '!', etc.
        // Values are normalized to 1000 units (PDF standard).
        public static int[] GetWidths(string filepath)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("Font file not found", filepath);

            byte[] data = File.ReadAllBytes(filepath);
            int[] widths = new int[95]; // Covers ASCII 32 to 126

            using (MemoryStream ms = new(data))
            using (BigEndianReader reader = new(ms))
            {
                // 1. READ OFFSET TABLE
                // https://learn.microsoft.com/en-us/typography/opentype/spec/otff
                reader.ReadFixed(); // version
                int numTables = reader.ReadUInt16();
                reader.Skip(6); // searchRange, entrySelector, rangeShift

                // 2. FIND TABLE OFFSETS
                Dictionary<string, uint> tables = new();
                for (int i = 0; i < numTables; i++)
                {
                    string tag = reader.ReadTag();
                    reader.ReadUInt32(); // checkSum
                    uint offset = reader.ReadUInt32();
                    reader.ReadUInt32(); // length
                    tables[tag] = offset;
                }

                if (!tables.TryGetValue("head", out uint headOffset) || !tables.TryGetValue("hmtx", out uint hmtxOffset) ||
                    !tables.TryGetValue("cmap", out uint cmapOffset) || !tables.TryGetValue("hhea", out uint hheaOffset))
                    throw new Exception("Invalid TTF: Missing critical tables.");

                // 3. READ HEAD TABLE (To get UnitsPerEm)
                reader.Seek(headOffset);
                reader.Skip(18);
                int unitsPerEm = reader.ReadUInt16();

                // 4. READ HHEA TABLE (To get NumberOfHMetrics)
                reader.Seek(hheaOffset);
                reader.Skip(34);
                int numberOfHMetrics = reader.ReadUInt16();

                // 5. READ CMAP (Character to Glyph Index Mapping)
                reader.Seek(cmapOffset);
                int glyphMapOffset = ReadCmapTable(reader, cmapOffset);

                // 6. READ HMTX (Glyph Index to Width)
                // We now iterate our target chars (32..126), look up their GlyphIndex, and retrieve width
                for (char c = ' '; c <= '~'; c++)
                {
                    int charIndex = c - 32;
                    int glyphIndex = GetGlyphIndex(reader, glyphMapOffset, c);

                    int rawWidth;
                    if (glyphIndex < numberOfHMetrics)
                    {
                        // The hmtx table is an array of LongHorMetric records (AdvanceWidth + LSB)
                        // Each record is 4 bytes (2 bytes width, 2 bytes lsb)
                        reader.Seek(hmtxOffset + glyphIndex * 4);
                        rawWidth = reader.ReadUInt16();
                    }
                    else
                    {
                        // If glyphIndex > numberOfHMetrics, it uses the width of the last metric
                        reader.Seek(hmtxOffset + (numberOfHMetrics - 1) * 4);
                        rawWidth = reader.ReadUInt16();
                    }

                    // Normalize to PDF 1000 units
                    // PDF Width = (RawWidth / UnitsPerEm) * 1000
                    widths[charIndex] = (int)(rawWidth / (float)unitsPerEm * 1000);
                }
            }

            return widths;
        }

        private static int ReadCmapTable(BigEndianReader reader, uint cmapStart)
        {
            reader.Seek(cmapStart);
            reader.ReadUInt16(); // version
            int numTables = reader.ReadUInt16();

            uint selectedOffset = 0;

            // Find Windows Unicode Platform (3, 1) or (3, 10)
            for (int i = 0; i < numTables; i++)
            {
                int platformId = reader.ReadUInt16();
                int encodingId = reader.ReadUInt16();
                uint offset = reader.ReadUInt32();

                if (platformId == 3 && (encodingId == 1 || encodingId == 10))
                {
                    selectedOffset = cmapStart + offset;
                    break;
                }
            }

            if (selectedOffset == 0)
                throw new Exception("Could not find Windows Unicode cmap.");
            return (int)selectedOffset;
        }

        private static int GetGlyphIndex(BigEndianReader reader, int cmapOffset, char c)
        {
            reader.Seek(cmapOffset);
            int format = reader.ReadUInt16();

            if (format != 4)
                return 0; // We only implement Format 4 (Standard Segment Mapping) for this demo

            reader.ReadUInt16(); // length
            reader.ReadUInt16(); // language
            int segCountX2 = reader.ReadUInt16();
            int segCount = segCountX2 / 2;
            reader.Skip(6); // searchRange, entrySelector, rangeShift

            // Arrays
            int[] endCounts = new int[segCount];
            for (int i = 0; i < segCount; i++)
                endCounts[i] = reader.ReadUInt16();

            reader.ReadUInt16(); // reservedPad

            int[] startCounts = new int[segCount];
            for (int i = 0; i < segCount; i++)
                startCounts[i] = reader.ReadUInt16();

            int[] idDeltas = new int[segCount]; // Signed!
            for (int i = 0; i < segCount; i++)
                idDeltas[i] = (short)reader.ReadUInt16();

            int idRangeOffsetPos = (int)reader.Position;
            int[] idRangeOffsets = new int[segCount];
            for (int i = 0; i < segCount; i++)
                idRangeOffsets[i] = reader.ReadUInt16();

            // Find segment
            for (int i = 0; i < segCount; i++)
                if (endCounts[i] >= c)
                {
                    if (startCounts[i] > c)
                        return 0; // Missing

                    if (idRangeOffsets[i] == 0) return (c + idDeltas[i]) & 0xFFFF;

                    // Obscure pointer math part of TTF spec
                    long glyphOffsetLoc = idRangeOffsetPos + i * 2 + idRangeOffsets[i] + 2 * (c - startCounts[i]);
                    reader.Seek(glyphOffsetLoc);
                    int glyphIndex = reader.ReadUInt16();
                    if (glyphIndex != 0)
                        return (glyphIndex + idDeltas[i]) & 0xFFFF;
                    return 0;
                }

            return 0;
        }
    }
}
