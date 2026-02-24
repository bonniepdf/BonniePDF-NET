using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BonniePDF.Typography;

/// <summary>
/// Creates a subset TrueType font containing only the glyphs that are actually used.
/// This significantly reduces embedded font size in PDFs.
/// </summary>
public class TtfSubsetter
{
    private readonly TtfFont _font;
    private readonly byte[] _originalData;
    private readonly Dictionary<string, (uint offset, uint length)> _tables;
    private short _indexToLocFormat; // 0 = short offsets, 1 = long offsets

    // Mapping from new GID to original GID
    private Dictionary<ushort, ushort> _newToOldGid;

    // Mapping from original GID to new GID in the subset
    private Dictionary<ushort, ushort> _oldToNewGid;

    // Head table info
    private int _unitsPerEm;

    public TtfSubsetter(TtfFont font)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _originalData = font.GetRawFontData();
        _tables = ParseTableDirectory();
        ReadHeadTable();
    }

    /// <summary>
    /// Gets the mapping from original GID to new GID after subsetting.
    /// Available after CreateSubset() is called.
    /// </summary>
    public IReadOnlyDictionary<ushort, ushort> OldToNewGidMap => _oldToNewGid;

    /// <summary>
    /// Gets the maximum original GID that was used (for CIDToGIDMap size calculation)
    /// </summary>
    public ushort MaxOriginalGid { get; private set; }

    /// <summary>
    /// Creates a subset font containing only the specified glyphs.
    /// </summary>
    /// <param name="usedGlyphIds">Set of glyph IDs to include in the subset</param>
    /// <param name="charToGlyphMap">Mapping of Unicode characters to glyph IDs</param>
    /// <returns>The subset font data as a byte array</returns>
    public byte[] CreateSubset(IReadOnlySet<ushort> usedGlyphIds, Dictionary<int, ushort> charToGlyphMap)
    {
        // 1. Build the GID remapping (always include GID 0 = .notdef)
        BuildGidMapping(usedGlyphIds);

        // 2. Get composite glyph dependencies (glyphs that reference other glyphs)
        HashSet<ushort> allGlyphs = GetAllRequiredGlyphs(usedGlyphIds);

        // Rebuild mapping with composite dependencies
        BuildGidMapping(allGlyphs);

        // 3. Build each required table
        Dictionary<string, byte[]> newTables = new()
        {
            // Required tables (must be present)
            ["head"] = BuildHeadTable(),
            ["hhea"] = BuildHheaTable(),
            ["maxp"] = BuildMaxpTable(),
            ["hmtx"] = BuildHmtxTable(),
            ["cmap"] = BuildCmapTable(charToGlyphMap)
        };

        // Glyph data tables
        (byte[] glyfData, byte[] locaData) = BuildGlyfAndLocaTables();
        newTables["glyf"] = glyfData;
        newTables["loca"] = locaData;

        // Optional tables - copy if present
        CopyTableIfPresent(newTables, "cvt ");
        CopyTableIfPresent(newTables, "fpgm");
        CopyTableIfPresent(newTables, "prep");
        CopyTableIfPresent(newTables, "post");
        CopyTableIfPresent(newTables, "name");
        CopyTableIfPresent(newTables, "OS/2");

        // 4. Assemble the final font file
        return AssembleFont(newTables);
    }

    #region Table Parsing

    private Dictionary<string, (uint offset, uint length)> ParseTableDirectory()
    {
        Dictionary<string, (uint offset, uint length)> tables = new();

        using MemoryStream ms = new(_originalData);
        using BinaryReader reader = new(ms);

        // Skip sfntVersion (4 bytes)
        reader.BaseStream.Seek(4, SeekOrigin.Begin);

        ushort numTables = ReadUInt16BE(reader);
        reader.BaseStream.Seek(12, SeekOrigin.Begin); // Skip to table directory

        for (int i = 0; i < numTables; i++)
        {
            string tag = Encoding.ASCII.GetString(reader.ReadBytes(4));
            uint checkSum = ReadUInt32BE(reader);
            uint offset = ReadUInt32BE(reader);
            uint length = ReadUInt32BE(reader);
            tables[tag] = (offset, length);
        }

        return tables;
    }

    private void ReadHeadTable()
    {
        if (!_tables.TryGetValue("head", out (uint offset, uint length) headInfo))
            throw new Exception("Missing required 'head' table");

        using MemoryStream ms = new(_originalData);
        using BinaryReader reader = new(ms);

        reader.BaseStream.Seek(headInfo.offset + 18, SeekOrigin.Begin);
        _unitsPerEm = ReadUInt16BE(reader);

        reader.BaseStream.Seek(headInfo.offset + 50, SeekOrigin.Begin);
        _indexToLocFormat = ReadInt16BE(reader);
    }

    #endregion

    #region GID Mapping

    private void BuildGidMapping(IReadOnlySet<ushort> usedGlyphs)
    {
        _oldToNewGid = new Dictionary<ushort, ushort>();
        _newToOldGid = new Dictionary<ushort, ushort>();

        // Always include GID 0 (.notdef)
        List<ushort> sortedGlyphs = usedGlyphs.OrderBy(g => g).ToList();
        if (!sortedGlyphs.Contains(0))
            sortedGlyphs.Insert(0, 0);

        // Track max original GID for CIDToGIDMap size
        MaxOriginalGid = sortedGlyphs.Count > 0 ? sortedGlyphs.Max() : (ushort)0;

        ushort newGid = 0;
        foreach (ushort oldGid in sortedGlyphs)
        {
            _oldToNewGid[oldGid] = newGid;
            _newToOldGid[newGid] = oldGid;
            newGid++;
        }
    }

    private HashSet<ushort> GetAllRequiredGlyphs(IReadOnlySet<ushort> usedGlyphs)
    {
        HashSet<ushort> allGlyphs = new(usedGlyphs);

        // Always include .notdef
        allGlyphs.Add(0);

        if (!_tables.TryGetValue("glyf", out (uint offset, uint length) glyfInfo) ||
            !_tables.TryGetValue("loca", out (uint offset, uint length) locaInfo))
            return allGlyphs;

        // Check each glyph for composite references
        Queue<ushort> toProcess = new(usedGlyphs);
        HashSet<ushort> processed = new();

        while (toProcess.Count > 0)
        {
            ushort gid = toProcess.Dequeue();
            if (processed.Contains(gid)) continue;
            processed.Add(gid);

            List<ushort> componentGids = GetCompositeComponents(gid, glyfInfo, locaInfo);
            foreach (ushort componentGid in componentGids)
                if (allGlyphs.Add(componentGid))
                    toProcess.Enqueue(componentGid);
        }

        return allGlyphs;
    }

    private List<ushort> GetCompositeComponents(ushort gid, (uint offset, uint length) glyfInfo,
        (uint offset, uint length) locaInfo)
    {
        List<ushort> components = new();

        // Get glyph offset from loca table
        uint glyphOffset, glyphLength;

        using MemoryStream ms = new(_originalData);
        using BinaryReader reader = new(ms);

        if (_indexToLocFormat == 0)
        {
            // Short format: offsets are ushort * 2
            reader.BaseStream.Seek(locaInfo.offset + gid * 2, SeekOrigin.Begin);
            uint offset1 = ReadUInt16BE(reader) * 2u;
            uint offset2 = ReadUInt16BE(reader) * 2u;
            glyphOffset = offset1;
            glyphLength = offset2 - offset1;
        }
        else
        {
            // Long format: offsets are uint
            reader.BaseStream.Seek(locaInfo.offset + gid * 4, SeekOrigin.Begin);
            uint offset1 = ReadUInt32BE(reader);
            uint offset2 = ReadUInt32BE(reader);
            glyphOffset = offset1;
            glyphLength = offset2 - offset1;
        }

        if (glyphLength == 0) return components; // Empty glyph

        // Read glyph header
        reader.BaseStream.Seek(glyfInfo.offset + glyphOffset, SeekOrigin.Begin);
        short numberOfContours = ReadInt16BE(reader);

        if (numberOfContours >= 0) return components; // Simple glyph, no components

        // Composite glyph - skip bounding box (8 bytes)
        reader.BaseStream.Seek(8, SeekOrigin.Current);

        // Parse component records
        const ushort ARG_1_AND_2_ARE_WORDS = 0x0001;
        const ushort MORE_COMPONENTS = 0x0020;
        const ushort WE_HAVE_A_SCALE = 0x0008;
        const ushort WE_HAVE_AN_X_AND_Y_SCALE = 0x0040;
        const ushort WE_HAVE_A_TWO_BY_TWO = 0x0080;

        ushort flags;
        do
        {
            flags = ReadUInt16BE(reader);
            ushort componentGid = ReadUInt16BE(reader);
            components.Add(componentGid);

            // Skip arguments
            if ((flags & ARG_1_AND_2_ARE_WORDS) != 0)
                reader.BaseStream.Seek(4, SeekOrigin.Current); // 2 shorts
            else
                reader.BaseStream.Seek(2, SeekOrigin.Current); // 2 bytes

            // Skip transformation
            if ((flags & WE_HAVE_A_SCALE) != 0)
                reader.BaseStream.Seek(2, SeekOrigin.Current);
            else if ((flags & WE_HAVE_AN_X_AND_Y_SCALE) != 0)
                reader.BaseStream.Seek(4, SeekOrigin.Current);
            else if ((flags & WE_HAVE_A_TWO_BY_TWO) != 0)
                reader.BaseStream.Seek(8, SeekOrigin.Current);
        } while ((flags & MORE_COMPONENTS) != 0);

        return components;
    }

    #endregion

    #region Table Building

    private byte[] BuildHeadTable()
    {
        if (!_tables.TryGetValue("head", out (uint offset, uint length) info))
            throw new Exception("Missing 'head' table");

        // Copy original head table
        byte[] data = new byte[info.length];
        Array.Copy(_originalData, info.offset, data, 0, info.length);

        // Update indexToLocFormat based on glyf table size
        // We'll use long format (1) for safety
        data[50] = 0;
        data[51] = 1; // indexToLocFormat = 1 (long)

        // Zero out checkSumAdjustment (will be recalculated if needed)
        data[8] = data[9] = data[10] = data[11] = 0;

        return data;
    }

    private byte[] BuildHheaTable()
    {
        if (!_tables.TryGetValue("hhea", out (uint offset, uint length) info))
            throw new Exception("Missing 'hhea' table");

        // Copy original hhea table
        byte[] data = new byte[info.length];
        Array.Copy(_originalData, info.offset, data, 0, info.length);

        // Update numberOfHMetrics to match subset glyph count
        ushort numGlyphs = (ushort)_newToOldGid.Count;
        data[34] = (byte)(numGlyphs >> 8);
        data[35] = (byte)(numGlyphs & 0xFF);

        return data;
    }

    private byte[] BuildMaxpTable()
    {
        if (!_tables.TryGetValue("maxp", out (uint offset, uint length) info))
            throw new Exception("Missing 'maxp' table");

        // Copy original maxp table
        byte[] data = new byte[info.length];
        Array.Copy(_originalData, info.offset, data, 0, info.length);

        // Update numGlyphs
        ushort numGlyphs = (ushort)_newToOldGid.Count;
        data[4] = (byte)(numGlyphs >> 8);
        data[5] = (byte)(numGlyphs & 0xFF);

        return data;
    }

    private byte[] BuildHmtxTable()
    {
        if (!_tables.TryGetValue("hmtx", out (uint offset, uint length) hmtxInfo))
            throw new Exception("Missing 'hmtx' table");

        int numGlyphs = _newToOldGid.Count;

        // Each glyph gets 4 bytes: advanceWidth (2) + leftSideBearing (2)
        byte[] data = new byte[numGlyphs * 4];

        using MemoryStream ms = new(_originalData);
        using BinaryReader reader = new(ms);

        // Read original hhea to get numberOfHMetrics
        if (!_tables.TryGetValue("hhea", out (uint offset, uint length) hheaInfo))
            throw new Exception("Missing 'hhea' table");

        reader.BaseStream.Seek(hheaInfo.offset + 34, SeekOrigin.Begin);
        int originalNumberOfHMetrics = ReadUInt16BE(reader);

        // Copy metrics for each glyph in the subset
        for (ushort newGid = 0; newGid < numGlyphs; newGid++)
        {
            ushort oldGid = _newToOldGid[newGid];

            ushort advanceWidth;
            short lsb;

            if (oldGid < originalNumberOfHMetrics)
            {
                // Full record
                reader.BaseStream.Seek(hmtxInfo.offset + oldGid * 4, SeekOrigin.Begin);
                advanceWidth = ReadUInt16BE(reader);
                lsb = ReadInt16BE(reader);
            }
            else
            {
                // Use last advanceWidth, read only lsb
                reader.BaseStream.Seek(hmtxInfo.offset + (originalNumberOfHMetrics - 1) * 4, SeekOrigin.Begin);
                advanceWidth = ReadUInt16BE(reader);

                int lsbOffset = originalNumberOfHMetrics * 4 + (oldGid - originalNumberOfHMetrics) * 2;
                reader.BaseStream.Seek(hmtxInfo.offset + lsbOffset, SeekOrigin.Begin);
                lsb = ReadInt16BE(reader);
            }

            // Write to new hmtx
            int offset = newGid * 4;
            data[offset] = (byte)(advanceWidth >> 8);
            data[offset + 1] = (byte)(advanceWidth & 0xFF);
            data[offset + 2] = (byte)(lsb >> 8);
            data[offset + 3] = (byte)(lsb & 0xFF);
        }

        return data;
    }

    private byte[] BuildCmapTable(Dictionary<int, ushort> charToGlyphMap)
    {
        // Split mappings into BMP and supplementary plane
        Dictionary<ushort, ushort> bmpMappings = new();
        Dictionary<uint, ushort> allMappings = new();

        foreach (KeyValuePair<int, ushort> kvp in charToGlyphMap)
        {
            int codepoint = kvp.Key;
            ushort oldGid = kvp.Value;

            if (!_oldToNewGid.TryGetValue(oldGid, out ushort newGid)) continue;

            allMappings[(uint)codepoint] = newGid;
            if (codepoint <= 0xFFFF)
                bmpMappings[(ushort)codepoint] = newGid;
        }

        bool hasSupplementary = allMappings.Keys.Any(cp => cp > 0xFFFF);

        if (hasSupplementary)
            return BuildCmapWithFormat12(bmpMappings, allMappings);

        return BuildCmapFormat4(bmpMappings);
    }

    private byte[] BuildCmapWithFormat12(Dictionary<ushort, ushort> bmpMappings, Dictionary<uint, ushort> allMappings)
    {
        // Build Format 4 subtable for BMP (compatibility with older viewers)
        byte[] format4 = BuildCmapFormat4Subtable(bmpMappings);
        // Build Format 12 subtable for full Unicode
        byte[] format12 = BuildCmapFormat12Subtable(allMappings);

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        // cmap header
        WriteUInt16BE(writer, 0); // version
        WriteUInt16BE(writer, 2); // numTables (Format 4 + Format 12)

        // Encoding records
        uint recordsSize = 4 + 2 * 8; // header (4) + 2 records (8 each)
        uint format4Offset = recordsSize;
        uint format12Offset = recordsSize + (uint)format4.Length;

        // Record 1: Windows Unicode BMP (3, 1) -> Format 4
        WriteUInt16BE(writer, 3);  // platformID
        WriteUInt16BE(writer, 1);  // encodingID
        WriteUInt32BE(writer, format4Offset);

        // Record 2: Windows Unicode full (3, 10) -> Format 12
        WriteUInt16BE(writer, 3);  // platformID
        WriteUInt16BE(writer, 10); // encodingID
        WriteUInt32BE(writer, format12Offset);

        writer.Write(format4);
        writer.Write(format12);

        return ms.ToArray();
    }

    private byte[] BuildCmapFormat12Subtable(Dictionary<uint, ushort> unicodeToGid)
    {
        // Build groups: consecutive codepoints with consecutive GIDs
        List<(uint startCharCode, uint endCharCode, uint startGlyphId)> groups = new();
        List<uint> sortedCodes = unicodeToGid.Keys.OrderBy(c => c).ToList();

        if (sortedCodes.Count > 0)
        {
            int i = 0;
            while (i < sortedCodes.Count)
            {
                uint startChar = sortedCodes[i];
                uint startGid = unicodeToGid[startChar];
                uint endChar = startChar;

                while (i + 1 < sortedCodes.Count)
                {
                    uint nextChar = sortedCodes[i + 1];
                    uint expectedGid = startGid + (nextChar - startChar);

                    if (nextChar == endChar + 1 && unicodeToGid[nextChar] == expectedGid)
                    {
                        endChar = nextChar;
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                groups.Add((startChar, endChar, startGid));
                i++;
            }
        }

        uint numGroups = (uint)groups.Count;
        uint subtableLength = 16 + numGroups * 12; // fixed header (16) + groups (12 each)

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        WriteUInt16BE(writer, 12);    // format
        WriteUInt16BE(writer, 0);     // reserved
        WriteUInt32BE(writer, subtableLength);
        WriteUInt32BE(writer, 0);     // language
        WriteUInt32BE(writer, numGroups);

        foreach ((uint startCharCode, uint endCharCode, uint startGlyphId) in groups)
        {
            WriteUInt32BE(writer, startCharCode);
            WriteUInt32BE(writer, endCharCode);
            WriteUInt32BE(writer, startGlyphId);
        }

        return ms.ToArray();
    }

    private byte[] BuildCmapFormat4Subtable(Dictionary<ushort, ushort> unicodeToGid)
    {
        List<(ushort startCode, ushort endCode, short idDelta)> segments = BuildCmapSegments(unicodeToGid);
        int segCount = segments.Count;
        int segCountX2 = segCount * 2;
        int searchRange = 2 * (1 << (int)Math.Floor(Math.Log(segCount, 2)));
        int entrySelector = (int)Math.Floor(Math.Log(segCount, 2));
        int rangeShift = segCountX2 - searchRange;
        int subtableLength = 16 + segCount * 8;

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        WriteUInt16BE(writer, 4); // format
        WriteUInt16BE(writer, (ushort)subtableLength);
        WriteUInt16BE(writer, 0); // language
        WriteUInt16BE(writer, (ushort)segCountX2);
        WriteUInt16BE(writer, (ushort)searchRange);
        WriteUInt16BE(writer, (ushort)entrySelector);
        WriteUInt16BE(writer, (ushort)rangeShift);

        foreach (var seg in segments) WriteUInt16BE(writer, seg.endCode);
        WriteUInt16BE(writer, 0); // reservedPad
        foreach (var seg in segments) WriteUInt16BE(writer, seg.startCode);
        foreach (var seg in segments) WriteInt16BE(writer, seg.idDelta);
        foreach (var seg in segments) WriteUInt16BE(writer, 0);

        return ms.ToArray();
    }

    private byte[] BuildCmapFormat4(Dictionary<ushort, ushort> unicodeToGid)
    {
        // BMP-only: single encoding record with Format 4 subtable
        byte[] subtable = BuildCmapFormat4Subtable(unicodeToGid);

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        // cmap header
        WriteUInt16BE(writer, 0); // version
        WriteUInt16BE(writer, 1); // numTables

        // Encoding record (Windows Unicode BMP)
        WriteUInt16BE(writer, 3);  // platformID
        WriteUInt16BE(writer, 1);  // encodingID
        WriteUInt32BE(writer, 12); // offset to subtable

        writer.Write(subtable);

        return ms.ToArray();
    }

    private List<(ushort startCode, ushort endCode, short idDelta)> BuildCmapSegments(
        Dictionary<ushort, ushort> unicodeToGid)
    {
        List<(ushort startCode, ushort endCode, short idDelta)> segments = new();

        if (unicodeToGid.Count == 0)
        {
            // Empty font - just add the required ending segment
            segments.Add((0xFFFF, 0xFFFF, 1));
            return segments;
        }

        List<ushort> sortedCodes = unicodeToGid.Keys.OrderBy(c => c).ToList();

        int i = 0;
        while (i < sortedCodes.Count)
        {
            ushort startCode = sortedCodes[i];
            ushort startGid = unicodeToGid[startCode];
            short idDelta = (short)(startGid - startCode);

            ushort endCode = startCode;

            // Extend segment while consecutive and same delta
            while (i + 1 < sortedCodes.Count)
            {
                ushort nextCode = sortedCodes[i + 1];
                ushort nextGid = unicodeToGid[nextCode];
                short nextDelta = (short)(nextGid - nextCode);

                if (nextCode == endCode + 1 && nextDelta == idDelta)
                {
                    endCode = nextCode;
                    i++;
                }
                else
                {
                    break;
                }
            }

            segments.Add((startCode, endCode, idDelta));
            i++;
        }

        // Add required ending segment
        segments.Add((0xFFFF, 0xFFFF, 1));

        return segments;
    }

    private (byte[] glyfData, byte[] locaData) BuildGlyfAndLocaTables()
    {
        if (!_tables.TryGetValue("glyf", out (uint offset, uint length) glyfInfo) ||
            !_tables.TryGetValue("loca", out (uint offset, uint length) locaInfo))
            throw new Exception("Missing 'glyf' or 'loca' table");

        int numGlyphs = _newToOldGid.Count;

        using MemoryStream glyfMs = new();
        using MemoryStream locaMs = new();
        using BinaryWriter glyfWriter = new(glyfMs);
        using BinaryWriter locaWriter = new(locaMs);

        using BinaryReader reader = new(new MemoryStream(_originalData));

        for (ushort newGid = 0; newGid < numGlyphs; newGid++)
        {
            // Write current offset to loca (long format)
            WriteUInt32BE(locaWriter, (uint)glyfMs.Position);

            ushort oldGid = _newToOldGid[newGid];

            // Get original glyph data location
            uint glyphOffset, glyphLength;

            if (_indexToLocFormat == 0)
            {
                reader.BaseStream.Seek(locaInfo.offset + oldGid * 2, SeekOrigin.Begin);
                uint offset1 = ReadUInt16BE(reader) * 2u;
                uint offset2 = ReadUInt16BE(reader) * 2u;
                glyphOffset = offset1;
                glyphLength = offset2 - offset1;
            }
            else
            {
                reader.BaseStream.Seek(locaInfo.offset + oldGid * 4, SeekOrigin.Begin);
                uint offset1 = ReadUInt32BE(reader);
                uint offset2 = ReadUInt32BE(reader);
                glyphOffset = offset1;
                glyphLength = offset2 - offset1;
            }

            if (glyphLength == 0) continue; // Empty glyph

            // Read original glyph data
            reader.BaseStream.Seek(glyfInfo.offset + glyphOffset, SeekOrigin.Begin);
            byte[] glyphData = reader.ReadBytes((int)glyphLength);

            // Check if composite and remap component GIDs
            short numberOfContours = (short)((glyphData[0] << 8) | glyphData[1]);
            if (numberOfContours < 0)
                // Composite glyph - remap component GIDs
                glyphData = RemapCompositeGlyph(glyphData);

            glyfWriter.Write(glyphData);

            // Pad to 4-byte boundary
            int padding = (4 - (int)glyfMs.Position % 4) % 4;
            for (int p = 0; p < padding; p++)
                glyfWriter.Write((byte)0);
        }

        // Write final offset to loca
        WriteUInt32BE(locaWriter, (uint)glyfMs.Position);

        return (glyfMs.ToArray(), locaMs.ToArray());
    }

    private byte[] RemapCompositeGlyph(byte[] glyphData)
    {
        byte[] result = (byte[])glyphData.Clone();

        const ushort ARG_1_AND_2_ARE_WORDS = 0x0001;
        const ushort MORE_COMPONENTS = 0x0020;
        const ushort WE_HAVE_A_SCALE = 0x0008;
        const ushort WE_HAVE_AN_X_AND_Y_SCALE = 0x0040;
        const ushort WE_HAVE_A_TWO_BY_TWO = 0x0080;

        int offset = 10; // Skip header (numberOfContours + bbox)

        ushort flags;
        do
        {
            flags = (ushort)((result[offset] << 8) | result[offset + 1]);
            ushort oldComponentGid = (ushort)((result[offset + 2] << 8) | result[offset + 3]);

            // Remap the component GID
            if (_oldToNewGid.TryGetValue(oldComponentGid, out ushort newComponentGid))
            {
                result[offset + 2] = (byte)(newComponentGid >> 8);
                result[offset + 3] = (byte)(newComponentGid & 0xFF);
            }

            offset += 4; // flags + glyphIndex

            // Skip arguments
            if ((flags & ARG_1_AND_2_ARE_WORDS) != 0)
                offset += 4;
            else
                offset += 2;

            // Skip transformation
            if ((flags & WE_HAVE_A_SCALE) != 0)
                offset += 2;
            else if ((flags & WE_HAVE_AN_X_AND_Y_SCALE) != 0)
                offset += 4;
            else if ((flags & WE_HAVE_A_TWO_BY_TWO) != 0)
                offset += 8;
        } while ((flags & MORE_COMPONENTS) != 0);

        return result;
    }

    private void CopyTableIfPresent(Dictionary<string, byte[]> newTables, string tag)
    {
        if (_tables.TryGetValue(tag, out (uint offset, uint length) info))
        {
            byte[] data = new byte[info.length];
            Array.Copy(_originalData, info.offset, data, 0, info.length);
            newTables[tag] = data;
        }
    }

    #endregion

    #region Font Assembly

    private byte[] AssembleFont(Dictionary<string, byte[]> tables)
    {
        // Calculate offsets and build the font
        int numTables = tables.Count;
        int searchRange = 16 * (1 << (int)Math.Floor(Math.Log(numTables, 2)));
        int entrySelector = (int)Math.Floor(Math.Log(numTables, 2));
        int rangeShift = numTables * 16 - searchRange;

        // Calculate header size: offset table (12) + table directory (numTables * 16)
        int headerSize = 12 + numTables * 16;

        // Sort tables alphabetically (required by TrueType spec for some tools)
        List<string> sortedTags = tables.Keys.OrderBy(t => t).ToList();

        // Calculate table offsets (4-byte aligned)
        Dictionary<string, uint> tableOffsets = new();
        uint currentOffset = (uint)headerSize;

        foreach (string tag in sortedTags)
        {
            tableOffsets[tag] = currentOffset;
            uint tableSize = (uint)tables[tag].Length;
            currentOffset += tableSize;
            // Align to 4 bytes
            currentOffset = (currentOffset + 3) & ~3u;
        }

        // Build the font
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        // Offset table
        WriteUInt32BE(writer, 0x00010000); // sfntVersion (TrueType)
        WriteUInt16BE(writer, (ushort)numTables);
        WriteUInt16BE(writer, (ushort)searchRange);
        WriteUInt16BE(writer, (ushort)entrySelector);
        WriteUInt16BE(writer, (ushort)rangeShift);

        // Table directory
        foreach (string tag in sortedTags)
        {
            byte[] tableData = tables[tag];
            uint checkSum = CalculateChecksum(tableData);

            writer.Write(Encoding.ASCII.GetBytes(tag.PadRight(4)));
            WriteUInt32BE(writer, checkSum);
            WriteUInt32BE(writer, tableOffsets[tag]);
            WriteUInt32BE(writer, (uint)tableData.Length);
        }

        // Write table data
        foreach (string tag in sortedTags)
        {
            byte[] tableData = tables[tag];
            writer.Write(tableData);

            // Pad to 4-byte boundary
            int padding = (4 - tableData.Length % 4) % 4;
            for (int i = 0; i < padding; i++)
                writer.Write((byte)0);
        }

        return ms.ToArray();
    }

    private uint CalculateChecksum(byte[] data)
    {
        uint sum = 0;
        int length = (data.Length + 3) / 4 * 4; // Round up to 4-byte boundary

        for (int i = 0; i < length; i += 4)
        {
            uint value = 0;
            for (int j = 0; j < 4 && i + j < data.Length; j++) value = (value << 8) | data[i + j];
            sum += value;
        }

        return sum;
    }

    #endregion

    #region Binary Helpers

    private static ushort ReadUInt16BE(BinaryReader reader)
    {
        byte[] b = reader.ReadBytes(2);
        return (ushort)((b[0] << 8) | b[1]);
    }

    private static short ReadInt16BE(BinaryReader reader)
    {
        byte[] b = reader.ReadBytes(2);
        return (short)((b[0] << 8) | b[1]);
    }

    private static uint ReadUInt32BE(BinaryReader reader)
    {
        byte[] b = reader.ReadBytes(4);
        return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
    }

    private static void WriteUInt16BE(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteInt16BE(BinaryWriter writer, short value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteUInt32BE(BinaryWriter writer, uint value)
    {
        writer.Write((byte)(value >> 24));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    #endregion
}