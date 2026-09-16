using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using BonniePDF.Rendering.Images;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Typography;

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
    internal static IDisposable BeginDocumentScope(bool embedStandardFonts)
    {
        DocumentAliasScope? previous = CurrentDocumentScope.Value;
        CurrentDocumentScope.Value = new DocumentAliasScope(embedStandardFonts);
        return new DocumentAliasScopeLease(previous);
    }

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
    public static void RegisterFontInstance(string logicalName, TtfFont font)
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
    public static float GetStringWidth(string text, string fontIdentifier, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        // A. IS IT A CUSTOM TTF FILE (direct path)?
        if (fontIdentifier.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
            fontIdentifier.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
            return GetTtfWidth(text, fontIdentifier, fontSize);

        DocumentAliasScope? scope = CurrentDocumentScope.Value;
        if (scope != null)
        {
            if (scope.InstanceAliases.TryGetValue(fontIdentifier, out TtfFont? scopedInstance))
                return GetKernedStringWidth(text, scopedInstance, fontSize);

            if (scope.PathAliases.TryGetValue(fontIdentifier, out string? scopedPath))
                return GetTtfWidth(text, scopedPath, fontSize);

            // PDF/A and PDF/UA embed bundled substitutes for the standard 12
            // Latin Type-1 fonts. Resolve that exact face before the first layout
            // measurement, rather than discovering it while drawing text.
            if (scope.EmbedStandardFonts && FontManager.GetParsedStandardFont(fontIdentifier) is TtfFont embedded)
            {
                scope.InstanceAliases[fontIdentifier] = embedded;
                return GetKernedStringWidth(text, embedded, fontSize);
            }

            return GetStandardFontWidth(text, fontIdentifier, fontSize);
        }

        // Outside document generation, preserve explicitly registered global aliases.
        if (GlobalFontInstanceAliases.TryGetValue(fontIdentifier, out TtfFont? instanceFont))
            return GetKernedStringWidth(text, instanceFont, fontSize);

        if (GlobalFontPathAliases.TryGetValue(fontIdentifier, out string? aliasedPath))
            return GetTtfWidth(text, aliasedPath, fontSize);

        // C. IS IT A STANDARD FONT? (approximate hardcoded tables)
        return GetStandardFontWidth(text, fontIdentifier, fontSize);
    }

    /// <summary>
    /// Measures the width of a string using actual per-glyph advances and kerning
    /// from an embedded TrueType font. More accurate than <see cref="GetStringWidth"/> for
    /// embedded fonts because it accounts for kern pairs.
    /// Returns 0 if font is null or text is empty.
    /// </summary>
    public static float GetKernedStringWidth(string text, TtfFont font, float fontSize)
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
        double scale = (double)fontSize / font.UnitsPerEm;
        double width = 0;

        ushort prevGid = 0;
        bool hasPrev = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (Rune.IsControl(rune)) continue;

            ushort gid = font.GetGlyphId(rune.Value);

            if (hasPrev && font.HasKerning)
            {
                short kern = font.GetKerning(prevGid, gid);
                width += kern * scale;
            }

            width += font.GetGlyphWidth(gid) * scale;
            prevGid = gid;
            hasPrev = true;
        }

        return (float)width;
    }

    // =====================================================
    // 2. CUSTOM TTF LOGIC
    // =====================================================
    private static float GetTtfWidth(string text, string filepath, float fontSize)
    {
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
                    return new TtfFont(path);
                }
                catch
                {
                    TtfFontLoadFailed.TryAdd(path, 0);
                    return null;
                }
            });
            if (fullFont != null)
                return GetKernedStringWidth(text, fullFont, fontSize);
        }

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
        if (fontName.Contains("Courier")) return text.Length * (fontSize * 0.6f);

        int[]? table = GetStandardFontTable(fontName);
        float totalWidth = 0;
        foreach (char c in text)
        {
            int index = c - 32;
            int w = table != null && index >= 0 && index < table.Length ? table[index] : 500;
            totalWidth += w;
        }
        return totalWidth / 1000f * fontSize;
    }

    // =====================================================
    // 4. TTF READER
    // =====================================================

    /// <summary>
    /// Simple TTF font reader that extracts character widths for ASCII characters
    /// </summary>
    public class SimpleTtfReader
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
                float version = reader.ReadFixed();
                int numTables = reader.ReadUInt16();
                reader.Skip(6); // searchRange, entrySelector, rangeShift

                // 2. FIND TABLE OFFSETS
                Dictionary<string, uint> tables = new();
                for (int i = 0; i < numTables; i++)
                {
                    string tag = reader.ReadTag();
                    uint checkSum = reader.ReadUInt32();
                    uint offset = reader.ReadUInt32();
                    uint length = reader.ReadUInt32();
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

            int length = reader.ReadUInt16();
            int language = reader.ReadUInt16();
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
