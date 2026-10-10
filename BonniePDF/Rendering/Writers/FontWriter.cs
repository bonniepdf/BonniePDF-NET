using BonniePDF.Rendering.Pdf;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Writes font objects to PDF, handling both standard Type1 fonts and embedded TrueType fonts.
/// Supports both synchronous (IPdfWriter) and asynchronous (IAsyncPdfWriter) operations.
/// </summary>
internal class FontWriter
{
    // Exactly one is non-null, depending on which constructor was used - see
    // StructureTreeWriter for the same pattern.
    private readonly IAsyncPdfWriter? _asyncWriter;
    private readonly IPdfWriter? _writer;

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
        _writer!.WriteObject(id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Font",
            [PdfKeys.Common.Subtype] = "/Type1",
            [PdfKeys.Font.BaseFont] = $"/{fontName}"
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
        int cidFontId = _writer!.GetNewId();
        int fontDescriptorId = _writer!.GetNewId();
        int fontFile2Id = _writer!.GetNewId();
        int toUnicodeId = _writer!.GetNewId();

        string baseFontName = fontInfo.SubsetFontName;

        // 1. Write FontFile2 (embedded TTF binary data)
        byte[] fontData;
        TtfSubsetter? subsetter = null;
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

        var codes=tracker.CidToGlyphMap;
        var cidMap=new Dictionary<ushort,ushort>(subsetter?.OldToNewGidMap??codes);
        foreach(var entry in codes)cidMap[entry.Key]=subsetter?.OldToNewGidMap?[entry.Value]??entry.Value;
        ushort maxCid=cidMap.Keys.Max();

        // 2. Write CIDSet (PDF/A-1b + PDF/UA-1). Mirrors the CIDToGIDMap
        //    domain (0..MaxOriginalGid) - see BuildCIDSetData.
        int cidSetId = 0;
        if (enableSubsetting && subsetter != null)
        {
            cidSetId = _writer!.GetNewId();
            WriteCIDSet(cidSetId, maxCid);
        }

        // 3. Write FontDescriptor
        WriteFontDescriptor(fontDescriptorId, font, baseFontName, fontFile2Id, cidSetId);

        // 4. Write CIDToGIDMap (maps original GIDs to new subset GIDs)
        string cidToGidMapRef;
        if (subsetter is not null || codes.Any(e=>e.Key!=e.Value))
        {
            int cidToGidMapId = _writer!.GetNewId();
            WriteCIDToGIDMap(cidToGidMapId, cidMap, maxCid);
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
        foreach(var entry in tracker.GlyphSources) cmapBuilder.AddSourceMapping(entry.Key,entry.Value);
        cmapBuilder.AddMappings(tracker.GetUsedCharToGlyphMap());
        WriteToUnicodeCMap(toUnicodeId, cmapBuilder.Build());

        // 7. Write Type0 font (the main font dictionary)
        _writer!.WriteObject(type0Id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Font",
            [PdfKeys.Common.Subtype] = "/Type0",
            [PdfKeys.Font.BaseFont] = $"/{baseFontName}",
            [PdfKeys.Font.Encoding] = "/Identity-H",
            [PdfKeys.Font.DescendantFonts] = $"[ {cidFontId} 0 R ]",
            [PdfKeys.Font.ToUnicode] = $"{toUnicodeId} 0 R"
        });

        return type0Id;
    }

    /// <summary>
    /// Writes the FontFile2 stream (embedded TTF binary data)
    /// </summary>
    private void WriteFontFile2(int id, byte[] fontData)
    {
        _writer!.WriteCompressedBinaryObject(id, new Dictionary<string, string>
        {
            [PdfKeys.Stream.DecodedLength] = fontData.Length.ToString()
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
            [PdfKeys.Common.Type] = "/FontDescriptor",
            [PdfKeys.FontDescriptor.Name] = $"/{baseFontName}",
            [PdfKeys.FontDescriptor.Flags] = font.Flags.ToString(),
            [PdfKeys.FontDescriptor.BoundingBox] = $"[ {bbox[0]} {bbox[1]} {bbox[2]} {bbox[3]} ]",
            [PdfKeys.FontDescriptor.ItalicAngle] = font.ItalicAngle.ToString(CultureInfo.InvariantCulture),
            [PdfKeys.FontDescriptor.Ascent] = ascent.ToString(),
            [PdfKeys.FontDescriptor.Descent] = descent.ToString(),
            [PdfKeys.FontDescriptor.CapHeight] = capHeight.ToString(),
            [PdfKeys.FontDescriptor.VerticalStemWidth] = stemV.ToString(),
            [PdfKeys.FontDescriptor.TrueTypeProgram] = $"{fontFile2Id} 0 R"
        };

        if (cidSetId > 0)
            dict[PdfKeys.FontDescriptor.CidSet] = $"{cidSetId} 0 R";

        _writer!.WriteObject(id, dict);
    }

    /// <summary>
    /// Writes the CIDSet stream - a bit-field identifying which CIDs are present
    /// in the embedded CIDFont file. Required for PDF/A-1b compliance (ISO 19005-1:2005, 6.3.5).
    /// Each bit corresponds to a CID; bit 7 of byte 0 = CID 0, bit 6 = CID 1, etc.
    /// </summary>
    private void WriteCIDSet(int id, int maxCid)
    {
        byte[] cidSetData = BuildCIDSetData(maxCid);
        _writer!.WriteCompressedBinaryObject(id, new Dictionary<string, string>(), cidSetData);
    }

    /// <summary>
    /// Builds the CIDSet bit-field for a CIDFontType2 subset.
    ///
    /// The CIDSet must mirror the CIDToGIDMap's domain (CIDs 0..maxCid). VeraPDF
    /// (PDF/UA-1, clause 7.21.4.2) treats every in-range CID as "present" - an
    /// unmapped CIDToGIDMap entry points at GID 0, which is &lt; numGlyphs - and
    /// requires the CIDSet to mark all of them, so set every bit 0..maxCid.
    /// Bit ordering: bit 7 of byte 0 = CID 0, bit 6 of byte 0 = CID 1, etc.
    /// </summary>
    private static byte[] BuildCIDSetData(int maxCid)
    {
        if (maxCid < 0) return Array.Empty<byte>();

        int byteCount = (maxCid / 8) + 1;
        byte[] data = new byte[byteCount];
        for (int i = 0; i < byteCount; i++) data[i] = 0xFF;

        // Zero the bits past maxCid in the final byte so the bitset length is
        // exactly maxCid + 1.
        int bitsInLastByte = (maxCid % 8) + 1;
        if (bitsInLastByte < 8)
            data[byteCount - 1] = (byte)((0xFF << (8 - bitsInLastByte)) & 0xFF);

        return data;
    }

    /// <summary>
    /// Writes the CIDFont (CIDFontType2) dictionary
    /// </summary>
    private void WriteCIDFont(int id, string baseFontName, int fontDescriptorId, string widthArray,
        string cidToGidMapRef)
    {
        _writer!.WriteObject(id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Font",
            [PdfKeys.Common.Subtype] = "/CIDFontType2",
            [PdfKeys.Font.BaseFont] = $"/{baseFontName}",
            [PdfKeys.Font.CidSystemInfo] = "<< /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>",
            [PdfKeys.Font.Descriptor] = $"{fontDescriptorId} 0 R",
            [PdfKeys.Font.Widths] = widthArray,
            [PdfKeys.Font.CidToGlyphMap] = cidToGidMapRef
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

        _writer!.WriteCompressedBinaryObject(id, new Dictionary<string, string>(), mapData);
    }

    /// <summary>
    /// Writes the ToUnicode CMap stream
    /// </summary>
    private void WriteToUnicodeCMap(int id, string cmapContent)
    {
        _writer!.WriteObject(id, new Dictionary<string, string>(), cmapContent);
    }

    /// <summary>
    /// Generates the /W (width) array for the CIDFont.
    /// Format: [ gid [w1 w2 w3...] gid [w1 w2...] ... ]
    /// </summary>
    private static string GenerateWidthArray(TtfFont font, GlyphUsageTracker tracker)
    {
        double scale = 1000d / font.UnitsPerEm;
        StringBuilder sb = new("[ ");
        var codes=tracker.CidToGlyphMap;
        List<ushort> glyphs = codes.Keys.Order().ToList();

        int i = 0;
        while (i < glyphs.Count)
        {
            ushort startGid = glyphs[i];
            sb.Append(startGid).Append(" [ ");

            int j = i;
            while (j < glyphs.Count && glyphs[j] == startGid + (j - i))
            {
                string scaledWidth = BonniePDF.Rendering.Pdf.PdfSyntax.FormatDecimal((float)(font.GetGlyphWidth(codes[glyphs[j]]) * scale));
                sb.Append(scaledWidth).Append(' ');
                j++;
            }

            sb.Append("] ");
            i = j; // Jump to the next non-consecutive GID
        }

        sb.Append(']');
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

        await _asyncWriter!.WriteObjectAsync(id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Font",
            [PdfKeys.Common.Subtype] = "/Type1",
            [PdfKeys.Font.BaseFont] = $"/{fontName}"
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
        int cidFontId = _asyncWriter!.GetNewId();
        int fontDescriptorId = _asyncWriter!.GetNewId();
        int fontFile2Id = _asyncWriter!.GetNewId();
        int toUnicodeId = _asyncWriter!.GetNewId();

        string baseFontName = fontInfo.SubsetFontName;

        // 1. Write FontFile2 (embedded TTF binary data)
        byte[] fontData;
        TtfSubsetter? subsetter = null;
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

        var codes=tracker.CidToGlyphMap;
        var cidMap=new Dictionary<ushort,ushort>(subsetter?.OldToNewGidMap??codes);
        foreach(var entry in codes)cidMap[entry.Key]=subsetter?.OldToNewGidMap?[entry.Value]??entry.Value;
        ushort maxCid=cidMap.Keys.Max();

        // 2. Write CIDSet (PDF/A-1b + PDF/UA-1). Mirrors the CIDToGIDMap
        //    domain (0..MaxOriginalGid) - see BuildCIDSetData.
        int cidSetId = 0;
        if (enableSubsetting && subsetter != null)
        {
            cidSetId = _asyncWriter!.GetNewId();
            await WriteCIDSetAsync(cidSetId, maxCid, cancellationToken).ConfigureAwait(false);
        }

        // 3. Write FontDescriptor
        await WriteFontDescriptorAsync(fontDescriptorId, font, baseFontName, fontFile2Id, cidSetId, cancellationToken)
            .ConfigureAwait(false);

        // 4. Write CIDToGIDMap (maps original GIDs to new subset GIDs)
        string cidToGidMapRef;
        if (subsetter is not null || codes.Any(e=>e.Key!=e.Value))
        {
            int cidToGidMapId = _asyncWriter!.GetNewId();
            await WriteCIDToGIDMapAsync(cidToGidMapId, cidMap, maxCid,
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
        foreach(var entry in tracker.GlyphSources) cmapBuilder.AddSourceMapping(entry.Key,entry.Value);
        cmapBuilder.AddMappings(tracker.GetUsedCharToGlyphMap());
        await WriteToUnicodeCMapAsync(toUnicodeId, cmapBuilder.Build(), cancellationToken).ConfigureAwait(false);

        // 6. Write Type0 font (the main font dictionary)
        await _asyncWriter!.WriteObjectAsync(type0Id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Font",
            [PdfKeys.Common.Subtype] = "/Type0",
            [PdfKeys.Font.BaseFont] = $"/{baseFontName}",
            [PdfKeys.Font.Encoding] = "/Identity-H",
            [PdfKeys.Font.DescendantFonts] = $"[ {cidFontId} 0 R ]",
            [PdfKeys.Font.ToUnicode] = $"{toUnicodeId} 0 R"
        }, null, cancellationToken).ConfigureAwait(false);

        return type0Id;
    }

    /// <summary>
    /// Writes the FontFile2 stream asynchronously (embedded TTF binary data)
    /// </summary>
    private async Task WriteFontFile2Async(int id, byte[] fontData, CancellationToken cancellationToken)
    {
        await _asyncWriter!.WriteCompressedBinaryObjectAsync(id, new Dictionary<string, string>
            {
                // /Length1 is specific to FontFile2 (TrueType) and represents the length of the font file itself
                [PdfKeys.Stream.DecodedLength] = fontData.Length.ToString()
            },
            fontData,
            fontData.Length,
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
            [PdfKeys.Common.Type] = "/FontDescriptor",
            [PdfKeys.FontDescriptor.Name] = $"/{baseFontName}",
            [PdfKeys.FontDescriptor.Flags] = font.Flags.ToString(),
            [PdfKeys.FontDescriptor.BoundingBox] = $"[ {bbox[0]} {bbox[1]} {bbox[2]} {bbox[3]} ]",
            [PdfKeys.FontDescriptor.ItalicAngle] = font.ItalicAngle.ToString(CultureInfo.InvariantCulture),
            [PdfKeys.FontDescriptor.Ascent] = ascent.ToString(),
            [PdfKeys.FontDescriptor.Descent] = descent.ToString(),
            [PdfKeys.FontDescriptor.CapHeight] = capHeight.ToString(),
            [PdfKeys.FontDescriptor.VerticalStemWidth] = stemV.ToString(),
            [PdfKeys.FontDescriptor.TrueTypeProgram] = $"{fontFile2Id} 0 R"
        };

        if (cidSetId > 0)
            dict[PdfKeys.FontDescriptor.CidSet] = $"{cidSetId} 0 R";

        await _asyncWriter!.WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the CIDSet stream asynchronously
    /// </summary>
    private async Task WriteCIDSetAsync(int id, int maxCid, CancellationToken cancellationToken)
    {
        byte[] cidSetData = BuildCIDSetData(maxCid);
        await _asyncWriter!.WriteCompressedBinaryObjectAsync(id, new Dictionary<string, string>(), cidSetData,
            cidSetData.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the CIDFont (CIDFontType2) dictionary asynchronously
    /// </summary>
    private async Task WriteCIDFontAsync(int id, string baseFontName, int fontDescriptorId, string widthArray,
        string cidToGidMapRef, CancellationToken cancellationToken)
    {
        await _asyncWriter!.WriteObjectAsync(id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Font",
            [PdfKeys.Common.Subtype] = "/CIDFontType2",
            [PdfKeys.Font.BaseFont] = $"/{baseFontName}",
            [PdfKeys.Font.CidSystemInfo] = "<< /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>",
            [PdfKeys.Font.Descriptor] = $"{fontDescriptorId} 0 R",
            [PdfKeys.Font.Widths] = widthArray,
            [PdfKeys.Font.CidToGlyphMap] = cidToGidMapRef
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
            await _asyncWriter!.WriteCompressedBinaryObjectAsync(
                id,
                new Dictionary<string, string>(),
                rentedBuffer,
                mapSize, // <--- Only compress/write the valid map data, not the whole buffer
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
        await _asyncWriter!.WriteObjectAsync(id, new Dictionary<string, string>(), cmapContent, cancellationToken)
            .ConfigureAwait(false);
    }
}