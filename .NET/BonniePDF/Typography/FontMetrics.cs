using System;
using System.Collections.Generic;
using System.IO;
using BonniePDF.IO;

namespace BonniePDF.Typography;

/// <summary>
/// Provides font metrics for calculating text width in PDF documents.
/// Supports both standard PDF fonts and custom TTF fonts.
/// </summary>
public static class FontMetrics
{
    // Cache for parsed TTF widths: "C:/Fonts/arial.ttf" -> int[95]
    private static readonly Dictionary<string, int[]> _ttfCache = [];

    // -----------------------------------------------------------------------
    // RAW METRICS: Widths for ASCII 32 (' ') to 126 ('~') in 1/1000th of EM
    // -----------------------------------------------------------------------

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

        // A. IS IT A CUSTOM TTF FILE?
        if (fontIdentifier.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            return GetTtfWidth(text, fontIdentifier, fontSize);

        // B. IS IT A STANDARD FONT?
        return GetStandardFontWidth(text, fontIdentifier, fontSize);
    }

    // =====================================================
    // 2. CUSTOM TTF LOGIC
    // =====================================================
    private static float GetTtfWidth(string text, string filepath, float fontSize)
    {
        // 1. Load into cache if missing
        if (!_ttfCache.ContainsKey(filepath))
            try
            {
                _ttfCache[filepath] = SimpleTtfReader.GetWidths(filepath);
            }
            catch
            {
                // Fallback if file not found or corrupted
                return text.Length * (fontSize * 0.5f);
            }

        int[] widths = _ttfCache[filepath];
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
    private static float GetStandardFontWidth(string text, string fontName, float fontSize)
    {
        // 1. Courier (Monospace) - Fixed width of 600 units (0.6)
        if (fontName.Contains("Courier")) return text.Length * (fontSize * 0.6f);

        // 2. Decide which table to use
        // Note: Times-Bold needs a slightly different table in reality,
        // but for a lightweight engine, using the Base table + 5% multiplier is acceptable.
        int[] table = fontName.Contains("Times") ? TimesWidths : HelveticaWidths;

        // 3. Modifiers
        // Bold fonts are generally 2-5% wider.
        // Italic fonts often have the same advance width as Roman, just slanted.
        float multiplier = fontName.Contains("Bold") ? 1.05f : 1.0f;

        float totalWidth = 0;
        foreach (char c in text)
        {
            int index = c - 32;
            int w = index >= 0 && index < table.Length ? table[index] : 500;
            totalWidth += w;
        }

        return totalWidth / 1000f * fontSize * multiplier;
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

                if (!tables.ContainsKey("head") || !tables.ContainsKey("hmtx") ||
                    !tables.ContainsKey("cmap") || !tables.ContainsKey("hhea"))
                    throw new Exception("Invalid TTF: Missing critical tables.");

                // 3. READ HEAD TABLE (To get UnitsPerEm)
                reader.Seek(tables["head"]);
                reader.Skip(18);
                int unitsPerEm = reader.ReadUInt16();

                // 4. READ HHEA TABLE (To get NumberOfHMetrics)
                reader.Seek(tables["hhea"]);
                reader.Skip(34);
                int numberOfHMetrics = reader.ReadUInt16();

                // 5. READ CMAP (Character to Glyph Index Mapping)
                reader.Seek(tables["cmap"]);
                int glyphMapOffset = ReadCmapTable(reader, tables["cmap"]);

                // 6. READ HMTX (Glyph Index to Width)
                // We now iterate our target chars (32..126), look up their GlyphIndex, and retrieve width
                for (char c = ' '; c <= '~'; c++)
                {
                    int charIndex = c - 32;
                    int glyphIndex = GetGlyphIndex(reader, glyphMapOffset, c);

                    int rawWidth = 0;
                    if (glyphIndex < numberOfHMetrics)
                    {
                        // The hmtx table is an array of LongHorMetric records (AdvanceWidth + LSB)
                        // Each record is 4 bytes (2 bytes width, 2 bytes lsb)
                        reader.Seek(tables["hmtx"] + glyphIndex * 4);
                        rawWidth = reader.ReadUInt16();
                    }
                    else
                    {
                        // If glyphIndex > numberOfHMetrics, it uses the width of the last metric
                        reader.Seek(tables["hmtx"] + (numberOfHMetrics - 1) * 4);
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