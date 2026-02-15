using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Models;
using BonniePDF.Typography;

namespace BonniePDF.IO;

/// <summary>
/// Writes font objects to PDF, handling both standard Type1 fonts and embedded TrueType fonts.
/// Supports both synchronous (IPdfWriter) and asynchronous (IAsyncPdfWriter) operations.
/// </summary>
public class FontWriter
{
    private readonly IAsyncPdfWriter _asyncWriter;
    private readonly IPdfWriter _writer;

    /// <summary>
    /// Creates a FontWriter for synchronous operations
    /// </summary>
    public FontWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    /// <summary>
    /// Creates a FontWriter for asynchronous operations
    /// </summary>
    public FontWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    /// <summary>
    /// Writes a standard Type1 font reference (not embedded)
    /// </summary>
    public void WriteType1Font(int id, string fontName)
    {
        _writer.WriteObject(id, new Dictionary<string, string>
        {
            ["/Type"] = "/Font",
            ["/Subtype"] = "/Type1",
            ["/BaseFont"] = $"/{fontName}"
        });
    }

    /// <summary>
    /// Writes a complete embedded TrueType font structure (Type0 composite font).
    /// Uses font subsetting to reduce file size by only including used glyphs.
    /// </summary>
    /// <param name="type0Id">The object ID for the Type0 font</param>
    /// <param name="fontInfo">The embedded font information</param>
    /// <param name="enableSubsetting">Whether to subset the font (default: true)</param>
    /// <returns>The Type0 font object ID</returns>
    public int WriteEmbeddedTtfFont(int type0Id, EmbeddedFontInfo fontInfo, bool enableSubsetting = true)
    {
        TtfFont font = fontInfo.Font;
        GlyphUsageTracker tracker = fontInfo.Tracker;

        // Allocate object IDs for all components
        int cidFontId = _writer.GetNewId();
        int fontDescriptorId = _writer.GetNewId();
        int fontFile2Id = _writer.GetNewId();
        int toUnicodeId = _writer.GetNewId();

        string baseFontName = fontInfo.SubsetFontName;

        // 1. Write FontFile2 (embedded TTF binary data)
        byte[] fontData;
        TtfSubsetter subsetter = null;
        if (enableSubsetting)
        {
            // Create subset font with only the used glyphs
            subsetter = new TtfSubsetter(font);
            fontData = subsetter.CreateSubset(tracker.UsedGlyphIds, tracker.GetUsedCharToGlyphMap());
        }
        else
        {
            // Embed full font
            fontData = font.GetRawFontData();
        }

        WriteFontFile2(fontFile2Id, fontData);

        // 2. Write CIDSet (required for PDF/A-1b compliance)
        int cidSetId = 0;
        if (enableSubsetting)
        {
            cidSetId = _writer.GetNewId();
            WriteCIDSet(cidSetId, tracker.UsedGlyphIds);
        }

        // 3. Write FontDescriptor
        WriteFontDescriptor(fontDescriptorId, font, baseFontName, fontFile2Id, cidSetId);

        // 4. Write CIDToGIDMap (maps original GIDs to new subset GIDs)
        string cidToGidMapRef;
        if (enableSubsetting)
        {
            int cidToGidMapId = _writer.GetNewId();
            WriteCIDToGIDMap(cidToGidMapId, subsetter.OldToNewGidMap, subsetter.MaxOriginalGid);
            cidToGidMapRef = $"{cidToGidMapId} 0 R";
        }
        else
        {
            cidToGidMapRef = "/Identity";
        }

        // 5. Write CIDFont (CIDFontType2)
        string widthArray = GenerateWidthArray(font, tracker);
        WriteCIDFont(cidFontId, baseFontName, fontDescriptorId, widthArray, cidToGidMapRef);

        // 6. Write ToUnicode CMap
        ToUnicodeCMapBuilder cmapBuilder = new();
        cmapBuilder.AddMappings(tracker.GetUsedCharToGlyphMap());
        WriteToUnicodeCMap(toUnicodeId, cmapBuilder.Build());

        // 7. Write Type0 font (the main font dictionary)
        _writer.WriteObject(type0Id, new Dictionary<string, string>
        {
            ["/Type"] = "/Font",
            ["/Subtype"] = "/Type0",
            ["/BaseFont"] = $"/{baseFontName}",
            ["/Encoding"] = "/Identity-H",
            ["/DescendantFonts"] = $"[ {cidFontId} 0 R ]",
            ["/ToUnicode"] = $"{toUnicodeId} 0 R"
        });

        return type0Id;
    }

    /// <summary>
    /// Writes the FontFile2 stream (embedded TTF binary data)
    /// </summary>
    private void WriteFontFile2(int id, byte[] fontData)
    {
        _writer.WriteBinaryObject(id, new Dictionary<string, string>
        {
            ["/Length1"] = fontData.Length.ToString()
        }, fontData);
    }

    /// <summary>
    /// Writes the FontDescriptor dictionary
    /// </summary>
    private void WriteFontDescriptor(int id, TtfFont font, string baseFontName, int fontFile2Id, int cidSetId = 0)
    {
        // Scale metrics from font units to PDF units (1000 per em)
        float scale = 1000f / font.UnitsPerEm;

        int ascent = (int)(font.Ascender * scale);
        int descent = (int)(font.Descender * scale);
        int capHeight = (int)(font.CapHeight * scale);
        int stemV = font.StemV;

        // Scale bounding box
        int[] bbox = font.BBox.Select(v => (int)(v * scale)).ToArray();

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/FontDescriptor",
            ["/FontName"] = $"/{baseFontName}",
            ["/Flags"] = font.Flags.ToString(),
            ["/FontBBox"] = $"[ {bbox[0]} {bbox[1]} {bbox[2]} {bbox[3]} ]",
            ["/ItalicAngle"] = font.ItalicAngle.ToString(CultureInfo.InvariantCulture),
            ["/Ascent"] = ascent.ToString(),
            ["/Descent"] = descent.ToString(),
            ["/CapHeight"] = capHeight.ToString(),
            ["/StemV"] = stemV.ToString(),
            ["/FontFile2"] = $"{fontFile2Id} 0 R"
        };

        if (cidSetId > 0)
            dict["/CIDSet"] = $"{cidSetId} 0 R";

        _writer.WriteObject(id, dict);
    }

    /// <summary>
    /// Writes the CIDSet stream — a bit-field identifying which CIDs are present
    /// in the embedded CIDFont file. Required for PDF/A-1b compliance (ISO 19005-1:2005, 6.3.5).
    /// Each bit corresponds to a CID; bit 7 of byte 0 = CID 0, bit 6 = CID 1, etc.
    /// </summary>
    private void WriteCIDSet(int id, IReadOnlySet<ushort> usedGlyphIds)
    {
        byte[] cidSetData = BuildCIDSetData(usedGlyphIds);
        _writer.WriteBinaryObject(id, new Dictionary<string, string>(), cidSetData);
    }

    /// <summary>
    /// Builds the CIDSet bit-field byte array from a set of used glyph IDs.
    /// </summary>
    private static byte[] BuildCIDSetData(IReadOnlySet<ushort> usedGlyphIds)
    {
        // Find the maximum CID to determine the size of the bit-field
        ushort maxCid = 0;
        foreach (ushort gid in usedGlyphIds)
            if (gid > maxCid) maxCid = gid;

        // Allocate enough bytes: each byte covers 8 CIDs
        int byteCount = (maxCid / 8) + 1;
        byte[] data = new byte[byteCount];

        // Set bits for each used CID
        // Bit ordering: bit 7 of byte 0 = CID 0, bit 6 of byte 0 = CID 1, etc.
        foreach (ushort cid in usedGlyphIds)
        {
            int byteIndex = cid / 8;
            int bitIndex = 7 - (cid % 8);
            data[byteIndex] |= (byte)(1 << bitIndex);
        }

        return data;
    }

    /// <summary>
    /// Writes the CIDFont (CIDFontType2) dictionary
    /// </summary>
    private void WriteCIDFont(int id, string baseFontName, int fontDescriptorId, string widthArray,
        string cidToGidMapRef)
    {
        _writer.WriteObject(id, new Dictionary<string, string>
        {
            ["/Type"] = "/Font",
            ["/Subtype"] = "/CIDFontType2",
            ["/BaseFont"] = $"/{baseFontName}",
            ["/CIDSystemInfo"] = "<< /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>",
            ["/FontDescriptor"] = $"{fontDescriptorId} 0 R",
            ["/W"] = widthArray,
            ["/CIDToGIDMap"] = cidToGidMapRef
        });
    }

    /// <summary>
    /// Writes the CIDToGIDMap stream that maps original GIDs to new subset GIDs
    /// </summary>
    private void WriteCIDToGIDMap(int id, IReadOnlyDictionary<ushort, ushort> oldToNewGid, ushort maxOriginalGid)
    {
        // The CIDToGIDMap is a stream of 2-byte big-endian values
        // Index i contains the new GID for original GID i
        // Size = (maxOriginalGid + 1) * 2 bytes
        int mapSize = (maxOriginalGid + 1) * 2;
        byte[] mapData = new byte[mapSize];

        foreach (KeyValuePair<ushort, ushort> kvp in oldToNewGid)
        {
            ushort originalGid = kvp.Key;
            ushort newGid = kvp.Value;

            int offset = originalGid * 2;
            if (offset + 1 < mapData.Length)
            {
                mapData[offset] = (byte)(newGid >> 8);
                mapData[offset + 1] = (byte)(newGid & 0xFF);
            }
        }

        _writer.WriteBinaryObject(id, new Dictionary<string, string>(), mapData);
    }

    /// <summary>
    /// Writes the ToUnicode CMap stream
    /// </summary>
    private void WriteToUnicodeCMap(int id, string cmapContent)
    {
        _writer.WriteObject(id, new Dictionary<string, string>(), cmapContent);
    }

    /// <summary>
    /// Generates the /W (width) array for the CIDFont.
    /// Format: [ gid [w1 w2 w3...] gid [w1 w2...] ... ]
    /// </summary>
    private string GenerateWidthArray(TtfFont font, GlyphUsageTracker tracker)
    {
        float scale = 1000f / font.UnitsPerEm;
        StringBuilder sb = new("[ ");
        List<ushort> glyphs = tracker.UsedGlyphIds.OrderBy(g => g).ToList();

        int i = 0;
        while (i < glyphs.Count)
        {
            ushort startGid = glyphs[i];
            sb.Append(startGid).Append(" [ ");

            int j = i;
            while (j < glyphs.Count && glyphs[j] == startGid + (j - i))
            {
                int scaledWidth = (int)(font.GetGlyphWidth(glyphs[j]) * scale);
                sb.Append(scaledWidth).Append(" ");
                j++;
            }

            sb.Append("] ");
            i = j; // Jump to the next non-consecutive GID
        }

        sb.Append("]");
        return sb.ToString();
    }

    // ========== ASYNC METHODS ==========

    /// <summary>
    /// Writes a standard Type1 font reference asynchronously (not embedded)
    /// </summary>
    public async Task WriteType1FontAsync(int id, string fontName, CancellationToken cancellationToken = default)
    {
        if (_asyncWriter == null)
            throw new InvalidOperationException("AsyncFontWriter requires an IAsyncPdfWriter");

        await _asyncWriter.WriteObjectAsync(id, new Dictionary<string, string>
        {
            ["/Type"] = "/Font",
            ["/Subtype"] = "/Type1",
            ["/BaseFont"] = $"/{fontName}"
        }, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a complete embedded TrueType font structure asynchronously (Type0 composite font).
    /// Uses font subsetting to reduce file size by only including used glyphs.
    /// </summary>
    public async Task<int> WriteEmbeddedTtfFontAsync(int type0Id, EmbeddedFontInfo fontInfo,
        bool enableSubsetting = true, CancellationToken cancellationToken = default)
    {
        if (_asyncWriter == null)
            throw new InvalidOperationException("AsyncFontWriter requires an IAsyncPdfWriter");

        TtfFont font = fontInfo.Font;
        GlyphUsageTracker tracker = fontInfo.Tracker;

        // Allocate object IDs for all components
        int cidFontId = _asyncWriter.GetNewId();
        int fontDescriptorId = _asyncWriter.GetNewId();
        int fontFile2Id = _asyncWriter.GetNewId();
        int toUnicodeId = _asyncWriter.GetNewId();

        string baseFontName = fontInfo.SubsetFontName;

        // 1. Write FontFile2 (embedded TTF binary data)
        byte[] fontData;
        TtfSubsetter subsetter = null;
        if (enableSubsetting)
        {
            // Create subset font with only the used glyphs
            subsetter = new TtfSubsetter(font);
            fontData = subsetter.CreateSubset(tracker.UsedGlyphIds, tracker.GetUsedCharToGlyphMap());
        }
        else
        {
            // Embed full font
            fontData = font.GetRawFontData();
        }

        await WriteFontFile2Async(fontFile2Id, fontData, cancellationToken).ConfigureAwait(false);

        // 2. Write CIDSet (required for PDF/A-1b compliance)
        int cidSetId = 0;
        if (enableSubsetting)
        {
            cidSetId = _asyncWriter.GetNewId();
            await WriteCIDSetAsync(cidSetId, tracker.UsedGlyphIds, cancellationToken).ConfigureAwait(false);
        }

        // 3. Write FontDescriptor
        await WriteFontDescriptorAsync(fontDescriptorId, font, baseFontName, fontFile2Id, cidSetId, cancellationToken)
            .ConfigureAwait(false);

        // 4. Write CIDToGIDMap (maps original GIDs to new subset GIDs)
        string cidToGidMapRef;
        if (enableSubsetting && subsetter != null)
        {
            int cidToGidMapId = _asyncWriter.GetNewId();
            await WriteCIDToGIDMapAsync(cidToGidMapId, subsetter.OldToNewGidMap, subsetter.MaxOriginalGid,
                cancellationToken).ConfigureAwait(false);
            cidToGidMapRef = $"{cidToGidMapId} 0 R";
        }
        else
        {
            cidToGidMapRef = "/Identity";
        }

        // 4. Write CIDFont (CIDFontType2)
        string widthArray = GenerateWidthArray(font, tracker);
        await WriteCIDFontAsync(cidFontId, baseFontName, fontDescriptorId, widthArray, cidToGidMapRef,
            cancellationToken).ConfigureAwait(false);

        // 5. Write ToUnicode CMap
        ToUnicodeCMapBuilder cmapBuilder = new();
        cmapBuilder.AddMappings(tracker.GetUsedCharToGlyphMap());
        await WriteToUnicodeCMapAsync(toUnicodeId, cmapBuilder.Build(), cancellationToken).ConfigureAwait(false);

        // 6. Write Type0 font (the main font dictionary)
        await _asyncWriter.WriteObjectAsync(type0Id, new Dictionary<string, string>
        {
            ["/Type"] = "/Font",
            ["/Subtype"] = "/Type0",
            ["/BaseFont"] = $"/{baseFontName}",
            ["/Encoding"] = "/Identity-H",
            ["/DescendantFonts"] = $"[ {cidFontId} 0 R ]",
            ["/ToUnicode"] = $"{toUnicodeId} 0 R"
        }, null, cancellationToken).ConfigureAwait(false);

        return type0Id;
    }

    /// <summary>
    /// Writes the FontFile2 stream asynchronously (embedded TTF binary data)
    /// </summary>
    private async Task WriteFontFile2Async(int id, byte[] fontData, CancellationToken cancellationToken)
    {
        await _asyncWriter.WriteBinaryObjectAsync(id, new Dictionary<string, string>
            {
                // /Length1 is specific to FontFile2 (TrueType) and represents the length of the font file itself
                ["/Length1"] = fontData.Length.ToString()
            },
            fontData,
            fontData.Length, // <--- Explicit length now required
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the FontDescriptor dictionary asynchronously
    /// </summary>
    private async Task WriteFontDescriptorAsync(int id, TtfFont font, string baseFontName, int fontFile2Id,
        int cidSetId, CancellationToken cancellationToken)
    {
        // Scale metrics from font units to PDF units (1000 per em)
        float scale = 1000f / font.UnitsPerEm;

        int ascent = (int)(font.Ascender * scale);
        int descent = (int)(font.Descender * scale);
        int capHeight = (int)(font.CapHeight * scale);
        int stemV = font.StemV;

        // Scale bounding box
        int[] bbox = font.BBox.Select(v => (int)(v * scale)).ToArray();

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/FontDescriptor",
            ["/FontName"] = $"/{baseFontName}",
            ["/Flags"] = font.Flags.ToString(),
            ["/FontBBox"] = $"[ {bbox[0]} {bbox[1]} {bbox[2]} {bbox[3]} ]",
            ["/ItalicAngle"] = font.ItalicAngle.ToString(CultureInfo.InvariantCulture),
            ["/Ascent"] = ascent.ToString(),
            ["/Descent"] = descent.ToString(),
            ["/CapHeight"] = capHeight.ToString(),
            ["/StemV"] = stemV.ToString(),
            ["/FontFile2"] = $"{fontFile2Id} 0 R"
        };

        if (cidSetId > 0)
            dict["/CIDSet"] = $"{cidSetId} 0 R";

        await _asyncWriter.WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the CIDSet stream asynchronously
    /// </summary>
    private async Task WriteCIDSetAsync(int id, IReadOnlySet<ushort> usedGlyphIds, CancellationToken cancellationToken)
    {
        byte[] cidSetData = BuildCIDSetData(usedGlyphIds);
        await _asyncWriter.WriteBinaryObjectAsync(id, new Dictionary<string, string>(), cidSetData,
            cidSetData.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the CIDFont (CIDFontType2) dictionary asynchronously
    /// </summary>
    private async Task WriteCIDFontAsync(int id, string baseFontName, int fontDescriptorId, string widthArray,
        string cidToGidMapRef, CancellationToken cancellationToken)
    {
        await _asyncWriter.WriteObjectAsync(id, new Dictionary<string, string>
        {
            ["/Type"] = "/Font",
            ["/Subtype"] = "/CIDFontType2",
            ["/BaseFont"] = $"/{baseFontName}",
            ["/CIDSystemInfo"] = "<< /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>",
            ["/FontDescriptor"] = $"{fontDescriptorId} 0 R",
            ["/W"] = widthArray,
            ["/CIDToGIDMap"] = cidToGidMapRef
        }, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the CIDToGIDMap stream asynchronously that maps original GIDs to new subset GIDs
    /// </summary>
    private async Task WriteCIDToGIDMapAsync(int id, IReadOnlyDictionary<ushort, ushort> oldToNewGid,
        ushort maxOriginalGid, CancellationToken cancellationToken)
    {
        // The CIDToGIDMap is a stream of 2-byte big-endian values
        // Index i contains the new GID for original GID i
        // Size = (maxOriginalGid + 1) * 2 bytes
        int mapSize = (maxOriginalGid + 1) * 2;

        // RENT a buffer from the pool (usually 64KB - 128KB for fonts)
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(mapSize);

        try
        {
            // CRITICAL: Pooled arrays are "dirty" and may contain garbage data.
            // We must zero-initialize the range we are using so that any 
            // unmapped original GIDs default to 0 (which is .notdef in PDF).
            Array.Clear(rentedBuffer, 0, mapSize);

            foreach (KeyValuePair<ushort, ushort> kvp in oldToNewGid)
            {
                ushort originalGid = kvp.Key;
                ushort newGid = kvp.Value;

                int offset = originalGid * 2;

                // Check against mapSize, not rentedBuffer.Length
                if (offset + 1 < mapSize)
                {
                    rentedBuffer[offset] = (byte)(newGid >> 8);
                    rentedBuffer[offset + 1] = (byte)(newGid & 0xFF);
                }
            }

            // Pass the rented buffer and the EXACT mapSize
            await _asyncWriter.WriteBinaryObjectAsync(
                id,
                new Dictionary<string, string>(),
                rentedBuffer,
                mapSize, // <--- Only write the valid map data, not the whole buffer
                cancellationToken
            ).ConfigureAwait(false);
        }
        finally
        {
            // Always return the buffer to the pool
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// Writes the ToUnicode CMap stream asynchronously
    /// </summary>
    private async Task WriteToUnicodeCMapAsync(int id, string cmapContent, CancellationToken cancellationToken)
    {
        await _asyncWriter.WriteObjectAsync(id, new Dictionary<string, string>(), cmapContent, cancellationToken)
            .ConfigureAwait(false);
    }
}