using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BonniePDF.Typography.TrueType;

/// <summary>
/// TrueType font parser for PDF embedding.
/// Parses all tables required for font embedding and text extraction.
/// </summary>
internal class TtfFont
{
    private readonly Dictionary<int, ushort> _charToGlyph = new(); // Unicode codepoint -> GID

    // Glyph data
    private readonly Dictionary<ushort, int> _glyphWidths = new(); // GID -> width in font units

    // Raw font data for embedding
    private readonly byte[] _rawData;

    // Table directory
    private readonly Dictionary<string, (uint offset, uint length)> _tables = new();
    private int _numberOfHMetrics;

    // Kern pairs: (leftGid, rightGid) -> adjustment in font units
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _shapeGate = new();
#else
    private readonly object _shapeGate = new();
#endif
    private Typography.Engine.FontFace? _engineFace;
    internal Typography.Engine.FontFace EngineFace => _engineFace ??= new Typography.Engine.FontFace(_rawData);
    internal IReadOnlyList<Typography.Engine.GlyphInfo> Shape(string text, IReadOnlyDictionary<string, int>? features = null)
    {
        lock (_shapeGate) return EmbeddedTextShaping.Shape(this, text, features);
    }

    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public TtfFont(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Font file not found", filePath);

        FilePath = filePath;
        _rawData = File.ReadAllBytes(filePath);

        Parse();
    }

    /// <summary>
    /// Creates a <see cref="TtfFont"/> from raw TTF bytes without requiring a file on disk.
    /// Works in any environment (desktop, server, cloud, browser via Blazor).
    /// </summary>
    public static TtfFont LoadFromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var font = new TtfFont(bytes);
        return font;
    }

    private TtfFont(byte[] bytes)
    {
        FilePath = "<from bytes>";
        _rawData = bytes;
        Parse();
    }

    // A parsed font is read-only, so one parse can serve every document and thread that uses the font.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<byte[], TtfFont> ByBytes = new();
#if NET9_0_OR_GREATER
    private static readonly System.Threading.Lock FileCacheGate = new();
#else
    private static readonly object FileCacheGate = new();
#endif
    private static readonly Dictionary<string, (long Length, long Ticks, TtfFont Font)> ByFile = new(StringComparer.Ordinal);

    /// <summary>
    /// Like <see cref="LoadFromBytes"/>, but parses a given array once: the result is kept for as long as the
    /// array is alive, so a font registered once and used by every document is not re-parsed for each.
    /// </summary>
    public static TtfFont LoadFromBytesCached(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (FontCache.MaxFiles <= 0) return LoadFromBytes(bytes);
        return ByBytes.GetValue(bytes, static b => new TtfFont(b));
    }

    /// <summary>
    /// Like <c>new TtfFont(path)</c>, but reads and parses a font file once, and again only if the file changes.
    /// At most <see cref="FontCache.MaxFiles"/> files are kept; the least recently loaded is dropped first.
    /// </summary>
    public static TtfFont LoadFromFileCached(string filePath)
    {
        if (FontCache.MaxFiles <= 0) return new TtfFont(filePath);

        FileInfo file = new(filePath);
        if (!file.Exists)
            throw new FileNotFoundException("Font file not found", filePath);

        string key = file.FullName;
        long ticks = file.LastWriteTimeUtc.Ticks;
        lock (FileCacheGate)
        {
            if (ByFile.TryGetValue(key, out var hit) && hit.Length == file.Length && hit.Ticks == ticks)
                return hit.Font;
        }

        TtfFont font = new(filePath);
        lock (FileCacheGate)
        {
            ByFile.Remove(key);
            while (ByFile.Count >= FontCache.MaxFiles && ByFile.Count > 0)
                ByFile.Remove(ByFile.Keys.First());
            ByFile[key] = (file.Length, ticks, font);
        }

        return font;
    }

    internal static void ClearFileCache()
    {
        lock (FileCacheGate) ByFile.Clear();
    }

    // =====================================================
    // PUBLIC PROPERTIES (from parsed tables)
    // =====================================================

    /// <summary>Path to the font file</summary>
    public string FilePath { get; }

    /// <summary>PostScript name from the name table (e.g., "ArialMT")</summary>
    public string PostScriptName { get; private set; } = "Unknown";

    /// <summary>Font family name (e.g., "Arial")</summary>
    public string FontFamily { get; private set; } = "Unknown";

    /// <summary>Font subfamily (e.g., "Regular", "Bold")</summary>
    public string FontSubfamily { get; private set; } = "Regular";

    /// <summary>OS/2 <c>usWeightClass</c> (1-1000), or null when the font has no OS/2 table.</summary>
    public int? WeightClass { get; private set; }

    /// <summary>OS/2 <c>fsSelection</c> ITALIC (bit 0) or OBLIQUE (bit 9) flag; false without an OS/2 table.</summary>
    public bool SelectionItalic { get; private set; }

    /// <summary>Units per em (typically 1000 or 2048)</summary>
    public int UnitsPerEm { get; private set; } = 1000;

    /// <summary>Ascender in font units</summary>
    public int Ascender { get; private set; }

    /// <summary>Descender in font units (typically negative)</summary>
    public int Descender { get; private set; }

    /// <summary>Cap height in font units</summary>
    public int CapHeight { get; private set; }

    /// <summary>x-height in font units</summary>
    public int XHeight { get; private set; }

    /// <summary>Font bounding box [xMin, yMin, xMax, yMax]</summary>
    public int[] BBox { get; } = new int[4];

    /// <summary>Italic angle in degrees</summary>
    public float ItalicAngle { get; private set; }

    /// <summary>Stem vertical width (estimated)</summary>
    public int StemV { get; private set; } = 80;

    /// <summary>PDF font flags</summary>
    public int Flags { get; private set; } = 32; // Nonsymbolic by default

    /// <summary>Total number of glyphs in the font</summary>
    public int NumberOfGlyphs { get; private set; }

    // =====================================================
    // PUBLIC API
    // =====================================================

    /// <summary>
    /// Gets the glyph ID for a Unicode codepoint
    /// </summary>
    public ushort GetGlyphId(int codepoint)
    {
        return _charToGlyph.TryGetValue(codepoint, out ushort gid) ? gid : (ushort)0;
    }

    /// <summary>
    /// Gets the glyph ID for a Unicode character (BMP convenience overload)
    /// </summary>
    public ushort GetGlyphId(char c) => GetGlyphId((int)c);

    /// <summary>
    /// Gets the width of a glyph in font units
    /// </summary>
    public int GetGlyphWidth(ushort glyphId)
    {
        return _glyphWidths.TryGetValue(glyphId, out int width) ? width : 0;
    }

    /// <summary>
    /// Gets the width of a character in font units
    /// </summary>
    public int GetCharWidth(int codepoint)
    {
        ushort gid = GetGlyphId(codepoint);
        return GetGlyphWidth(gid);
    }

    /// <summary>
    /// Gets the width of a character in points
    /// </summary>
    public float GetCharWidthInPoints(int codepoint, float fontSize)
    {
        int width = GetCharWidth(codepoint);
        return width / (float)UnitsPerEm * fontSize;
    }

    /// <summary>
    /// Gets the kerning adjustment in font units between two adjacent glyphs.
    /// Returns 0 if no kerning pair exists or the font has no kern table.
    /// </summary>
    internal string GlyphOutline(int glyphId)
    {
        lock (_shapeGate) return GlyphOutlinePath.Build(EngineFace, glyphId);
    }

    public int GetKerning(ushort leftGid, ushort rightGid)
    {
        lock (_shapeGate) return EngineFace.GetPairKerning(leftGid, rightGid);
    }

    /// <summary>
    /// Returns true if this font has any kern pairs defined.
    /// </summary>
    public bool HasKerning => _tables.ContainsKey("kern") || _tables.ContainsKey("GPOS");

    /// <summary>
    /// Gets the raw font data for embedding
    /// </summary>
    public byte[] GetRawFontData()
    {
        return _rawData;
    }

    /// <summary>
    /// Gets a copy of the codepoint to glyph mapping (supports full Unicode range)
    /// </summary>
    public IReadOnlyDictionary<int, ushort> GetCharToGlyphMap()
    {
        return _charToGlyph;
    }

    /// <summary>
    /// Gets all glyph widths (for width array generation)
    /// </summary>
    public IReadOnlyDictionary<ushort, int> GetAllGlyphWidths()
    {
        return _glyphWidths;
    }

    // =====================================================
    // PARSING
    // =====================================================

    private void Parse()
    {
        using MemoryStream ms = new(_rawData);
        using BigEndianReader reader = new(ms);

        // 1. Read offset table
        reader.ReadFixed(); // version
        int numTables = reader.ReadUInt16();
        reader.Skip(6); // searchRange, entrySelector, rangeShift

        // 2. Read table directory
        for (int i = 0; i < numTables; i++)
        {
            string tag = reader.ReadTag();
            reader.ReadUInt32(); // checkSum
            uint offset = reader.ReadUInt32();
            uint length = reader.ReadUInt32();
            _tables[tag] = (offset, length);
        }

        // 3. Verify required tables exist
        string[] required = { "head", "hhea", "hmtx", "cmap", "maxp" };
        foreach (string table in required)
            if (!_tables.ContainsKey(table))
                throw new Exception($"Invalid TTF: Missing required table '{table}'");

        // 4. Parse tables in order
        ParseHeadTable(reader);
        ParseMaxpTable(reader);
        ParseHheaTable(reader);
        ParseHmtxTable(reader);
        ParseCmapTable(reader);

        // Optional tables
        if (_tables.ContainsKey("name"))
            ParseNameTable(reader);

        if (_tables.ContainsKey("OS/2"))
            ParseOS2Table(reader);

        if (_tables.ContainsKey("post"))
            ParsePostTable(reader);
    }

    private void ParseHeadTable(BigEndianReader reader)
    {
        reader.Seek(_tables["head"].offset);

        reader.Skip(4); // version
        reader.Skip(4); // fontRevision
        reader.Skip(4); // checkSumAdjustment
        reader.Skip(4); // magicNumber
        reader.Skip(2); // flags

        UnitsPerEm = reader.ReadUInt16();

        reader.Skip(8); // created
        reader.Skip(8); // modified

        BBox[0] = reader.ReadInt16(); // xMin
        BBox[1] = reader.ReadInt16(); // yMin
        BBox[2] = reader.ReadInt16(); // xMax
        BBox[3] = reader.ReadInt16(); // yMax
    }

    private void ParseMaxpTable(BigEndianReader reader)
    {
        reader.Seek(_tables["maxp"].offset);

        reader.Skip(4); // version
        NumberOfGlyphs = reader.ReadUInt16();
    }

    private void ParseHheaTable(BigEndianReader reader)
    {
        reader.Seek(_tables["hhea"].offset);

        reader.Skip(4); // version
        Ascender = reader.ReadInt16();
        Descender = reader.ReadInt16();
        reader.Skip(2); // lineGap
        reader.Skip(2); // advanceWidthMax
        reader.Skip(2); // minLeftSideBearing
        reader.Skip(2); // minRightSideBearing
        reader.Skip(2); // xMaxExtent
        reader.Skip(2); // caretSlopeRise
        reader.Skip(2); // caretSlopeRun
        reader.Skip(2); // caretOffset
        reader.Skip(8); // reserved
        reader.Skip(2); // metricDataFormat

        _numberOfHMetrics = reader.ReadUInt16();
    }

    private void ParseHmtxTable(BigEndianReader reader)
    {
        reader.Seek(_tables["hmtx"].offset);

        // Read all LongHorMetric records
        int lastWidth = 0;
        for (int i = 0; i < _numberOfHMetrics; i++)
        {
            int advanceWidth = reader.ReadUInt16();
            reader.Skip(2); // leftSideBearing
            _glyphWidths[(ushort)i] = advanceWidth;
            lastWidth = advanceWidth;
        }

        // Remaining glyphs use the last width
        for (int i = _numberOfHMetrics; i < NumberOfGlyphs; i++) _glyphWidths[(ushort)i] = lastWidth;
    }

    private void ParseCmapTable(BigEndianReader reader)
    {
        uint cmapStart = _tables["cmap"].offset;
        reader.Seek(cmapStart);

        reader.ReadUInt16(); // version
        int numSubtables = reader.ReadUInt16();

        // Find the best subtable (prefer Format 12, then Format 4)
        uint format4Offset = 0;
        uint format12Offset = 0;

        for (int i = 0; i < numSubtables; i++)
        {
            int platformId = reader.ReadUInt16();
            int encodingId = reader.ReadUInt16();
            uint offset = reader.ReadUInt32();

            // Windows Unicode BMP (3, 1) - Format 4
            if (platformId == 3 && encodingId == 1)
                format4Offset = cmapStart + offset;
            // Windows Unicode full repertoire (3, 10) - Format 12
            else if (platformId == 3 && encodingId == 10) format12Offset = cmapStart + offset;
        }

        // Try Format 12 first (full Unicode), then Format 4 (BMP only)
        if (format12Offset != 0)
            ParseCmapFormat12(reader, format12Offset);
        else if (format4Offset != 0)
            ParseCmapFormat4(reader, format4Offset);
        else
            throw new Exception("No supported cmap subtable found");
    }

    private void ParseCmapFormat4(BigEndianReader reader, uint offset)
    {
        reader.Seek(offset);

        int format = reader.ReadUInt16();
        if (format != 4) return;

        reader.ReadUInt16(); // length
        reader.ReadUInt16(); // language
        int segCountX2 = reader.ReadUInt16();
        int segCount = segCountX2 / 2;
        reader.Skip(6); // searchRange, entrySelector, rangeShift

        int[] endCounts = new int[segCount];
        for (int i = 0; i < segCount; i++)
            endCounts[i] = reader.ReadUInt16();

        reader.ReadUInt16(); // reservedPad

        int[] startCounts = new int[segCount];
        for (int i = 0; i < segCount; i++)
            startCounts[i] = reader.ReadUInt16();

        int[] idDeltas = new int[segCount];
        for (int i = 0; i < segCount; i++)
            idDeltas[i] = reader.ReadInt16();

        long idRangeOffsetPos = reader.Position;
        int[] idRangeOffsets = new int[segCount];
        for (int i = 0; i < segCount; i++)
            idRangeOffsets[i] = reader.ReadUInt16();

        // Build char to glyph mapping
        for (int seg = 0; seg < segCount; seg++)
        {
            int start = startCounts[seg];
            int end = endCounts[seg];

            if (start == 0xFFFF) continue; // End segment

            for (int charCode = start; charCode <= end; charCode++)
            {
                ushort glyphId;

                if (idRangeOffsets[seg] == 0)
                {
                    glyphId = (ushort)((charCode + idDeltas[seg]) & 0xFFFF);
                }
                else
                {
                    long glyphOffsetLoc = idRangeOffsetPos + seg * 2 + idRangeOffsets[seg] + 2 * (charCode - start);
                    reader.Seek(glyphOffsetLoc);
                    glyphId = reader.ReadUInt16();
                    if (glyphId != 0)
                        glyphId = (ushort)((glyphId + idDeltas[seg]) & 0xFFFF);
                }

                if (glyphId != 0) _charToGlyph[charCode] = glyphId;
            }
        }
    }

    private void ParseCmapFormat12(BigEndianReader reader, uint offset)
    {
        reader.Seek(offset);

        int format = reader.ReadUInt16();
        if (format != 12) return;

        reader.ReadUInt16(); // reserved
        reader.ReadUInt32(); // length
        reader.ReadUInt32(); // language
        uint numGroups = reader.ReadUInt32();

        for (uint i = 0; i < numGroups; i++)
        {
            uint startCharCode = reader.ReadUInt32();
            uint endCharCode = reader.ReadUInt32();
            uint startGlyphId = reader.ReadUInt32();

            for (uint charCode = startCharCode; charCode <= endCharCode; charCode++)
            {
                ushort glyphId = (ushort)(startGlyphId + (charCode - startCharCode));
                _charToGlyph[(int)charCode] = glyphId;
            }
        }
    }

    private void ParseNameTable(BigEndianReader reader)
    {
        reader.Seek(_tables["name"].offset);

        reader.ReadUInt16(); // format
        int count = reader.ReadUInt16();
        int stringOffset = reader.ReadUInt16();
        uint storageStart = _tables["name"].offset + (uint)stringOffset;

        for (int i = 0; i < count; i++)
        {
            int platformId = reader.ReadUInt16();
            int encodingId = reader.ReadUInt16();
            reader.ReadUInt16(); // languageId
            int nameId = reader.ReadUInt16();
            int length = reader.ReadUInt16();
            int offset = reader.ReadUInt16();

            // We want Windows Unicode (3, 1) or Mac Roman (1, 0)
            bool isWindowsUnicode = platformId == 3 && encodingId == 1;
            bool isMacRoman = platformId == 1 && encodingId == 0;

            if (!isWindowsUnicode && !isMacRoman) continue;

            long currentPos = reader.Position;
            reader.Seek(storageStart + offset);

            string value;
            if (isWindowsUnicode)
            {
                byte[] bytes = reader.ReadBytes(length);
                value = Encoding.BigEndianUnicode.GetString(bytes);
            }
            else
            {
                byte[] bytes = reader.ReadBytes(length);
                value = Encoding.ASCII.GetString(bytes);
            }

            reader.Seek(currentPos);

            switch (nameId)
            {
                case 1: FontFamily = value; break;
                case 2: FontSubfamily = value; break;
                case 6: PostScriptName = value; break;
            }
        }

        // Fallback: generate PostScript name if not found
        if (string.Equals(PostScriptName, "Unknown", StringComparison.Ordinal))
            PostScriptName = FontFamily.Replace(" ", "") + "-" + FontSubfamily.Replace(" ", "");
    }

    private void ParseOS2Table(BigEndianReader reader)
    {
        reader.Seek(_tables["OS/2"].offset);

        int version = reader.ReadUInt16();
        reader.Skip(2); // xAvgCharWidth
        int usWeightClass = reader.ReadUInt16();
        reader.ReadUInt16(); // usWidthClass
        reader.ReadUInt16(); // fsType

        reader.Skip(16); // subscript (8) + superscript (8) metrics

        reader.Skip(2); // yStrikeoutSize
        reader.Skip(2); // yStrikeoutPosition
        reader.Skip(2); // sFamilyClass

        reader.Skip(10); // panose

        reader.Skip(16); // ulUnicodeRange

        reader.Skip(4); // achVendID

        int fsSelection = reader.ReadUInt16();

        reader.Skip(4); // usFirstCharIndex, usLastCharIndex

        reader.ReadInt16(); // sTypoAscender
        reader.ReadInt16(); // sTypoDescender
        reader.ReadInt16(); // sTypoLineGap

        reader.Skip(4); // usWinAscent, usWinDescent

        // Read sxHeight and sCapHeight if version >= 2.
        // Per the OpenType OS/2 spec the field order is sxHeight THEN sCapHeight
        // (both follow the two ulCodePageRange uint32s).
        if (version >= 2)
        {
            reader.Skip(8); // ulCodePageRange1 + ulCodePageRange2
            XHeight = reader.ReadInt16();
            CapHeight = reader.ReadInt16();
        }
        else
        {
            // Estimate from UnitsPerEm
            CapHeight = (int)(UnitsPerEm * 0.7);
            XHeight = (int)(UnitsPerEm * 0.5);
        }

        // Calculate StemV from weight class (rough estimate)
        WeightClass = usWeightClass is >= 1 and <= 1000 ? usWeightClass : null;
        SelectionItalic = (fsSelection & 1) != 0 || (fsSelection & 0x200) != 0;

        StemV = 50 + usWeightClass / 10;

        // Build PDF flags
        Flags = 0;

        // Fixed pitch (check if all widths are the same)
        int distinctWidths = _glyphWidths.Values.Distinct().Count();
        if (distinctWidths <= 2) // Allow for .notdef
            Flags |= 1; // FixedPitch

        // Serif (check panose or name)
        // For now, assume sans-serif unless name contains "Serif" or "Times"
        if (FontFamily.Contains("Serif", StringComparison.OrdinalIgnoreCase) ||
            FontFamily.Contains("Times", StringComparison.OrdinalIgnoreCase) ||
            FontFamily.Contains("Georgia", StringComparison.OrdinalIgnoreCase))
            Flags |= 2; // Serif

        // Italic
        if ((fsSelection & 1) != 0)
            Flags |= 64; // Italic

        // Nonsymbolic (has standard Latin characters)
        if (_charToGlyph.ContainsKey((int)'A') && _charToGlyph.ContainsKey((int)'a'))
            Flags |= 32; // Nonsymbolic
        else
            Flags |= 4; // Symbolic
    }

    private void ParsePostTable(BigEndianReader reader)
    {
        reader.Seek(_tables["post"].offset);

        reader.Skip(4); // version
        ItalicAngle = reader.ReadFixed();
    }

}
