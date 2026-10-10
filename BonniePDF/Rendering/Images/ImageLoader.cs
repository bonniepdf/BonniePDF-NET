using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BonniePDF;

/// <summary>
/// Loads JPEG and PNG images and extracts metadata for PDF embedding
/// </summary>
public static class ImageLoader
{
    /// <summary>
    /// Load an image from file path, auto-detecting format
    /// </summary>
    public static ImageInfo Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found", filePath);

        byte[] data = File.ReadAllBytes(filePath);
        return Load(data);
    }

    /// <summary>
    /// Load an image from byte array, auto-detecting format
    /// </summary>
    public static ImageInfo Load(byte[] data)
    {
        using var performanceScope = PerformanceEvents.Measure(PerformanceStage.ImageDecoding);
        if (data == null || data.Length < 8)
            throw new ArgumentException("Image data is empty or too small", nameof(data));

        // Check for PNG signature (89 50 4E 47 0D 0A 1A 0A)
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return ParsePng(data);

        // Check for JPEG signature (FF D8 FF)
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return ParseJpeg(data);

        throw new InvalidOperationException("Unknown image format. Supported formats: JPEG, PNG");
    }

    /// <summary>
    /// Loads an image through the process-wide cache (see <see cref="ImagePipeline"/>): the file is read and
    /// decoded once however many documents place it, and re-read if it changes on disk. The result is a
    /// private copy of the cached description that shares its pixel arrays, which must not be modified.
    /// </summary>
    internal static ImageInfo LoadCached(string filePath)
    {
        FileInfo file = new(filePath);
        if (!file.Exists)
            throw new FileNotFoundException("Image file not found", filePath);

        string key = $"f|{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
        return ImageCache.GetOrAdd(key, () => WithContentKey(Load(File.ReadAllBytes(file.FullName)))).ShallowClone();
    }

    /// <summary>
    /// Loads an image from bytes through the process-wide cache, keyed by the bytes' content so a fresh copy
    /// of the same file in each document still hits. The result is a private copy of the cached description
    /// that shares its pixel arrays, which must not be modified.
    /// </summary>
    internal static ImageInfo LoadCached(byte[] data)
    {
        if (ImagePipeline.CacheMaxBytes <= 0 || data == null || data.Length < 8) return Load(data!);

        string key = "b|" + System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data), 0, 8);
        return ImageCache.GetOrAdd(key, () => WithContentKey(Load(data))).ShallowClone();
    }

    private static ImageInfo WithContentKey(ImageInfo info)
    {
        info.ContentKey = PdfResourceRegistry.ContentKeyOf(info);
        return info;
    }

    /// <summary>
    /// Load a JPEG image from file path
    /// </summary>
    public static ImageInfo LoadJpeg(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found", filePath);

        byte[] data = File.ReadAllBytes(filePath);
        return ParseJpeg(data);
    }

    /// <summary>
    /// Load a JPEG image from byte array
    /// </summary>
    public static ImageInfo LoadJpeg(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Image data is empty", nameof(data));

        return ParseJpeg(data);
    }

    /// <summary>
    /// Load a PNG image from file path
    /// </summary>
    public static ImageInfo LoadPng(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found", filePath);

        byte[] data = File.ReadAllBytes(filePath);
        return ParsePng(data);
    }

    /// <summary>
    /// Load a PNG image from byte array
    /// </summary>
    public static ImageInfo LoadPng(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Image data is empty", nameof(data));

        return ParsePng(data);
    }

    /// <summary>
    /// Parse JPEG file to extract dimensions and color space
    /// </summary>
    private static ImageInfo ParseJpeg(byte[] data)
    {
        // Verify JPEG signature (FF D8 FF)
        if (data.Length < 3 || data[0] != 0xFF || data[1] != 0xD8 || data[2] != 0xFF)
            throw new InvalidOperationException("Invalid JPEG file: Missing JPEG signature");

        ImageInfo info = new()
        {
            Data = data,
            OriginalData = data,
            Format = ImageFormat.Jpeg
        };

        // ICC profile spans 1..N APP2 segments, indexed by a sequence number.
        // Collect them here and assemble into info.IccProfile after the scan.
        List<(int Seq, byte[] Bytes)> iccChunks = new();
        int iccTotal = 0;

        // Parse JPEG segments to find the SOF (Start of Frame) marker plus
        // any APP2 (ICC profile) / APP14 (Adobe CMYK transform) metadata.
        int pos = 2;
        bool sofFound = false;

        while (pos < data.Length - 1)
        {
            // Find marker (0xFF)
            if (data[pos] != 0xFF)
            {
                pos++;
                continue;
            }

            byte marker = data[pos + 1];

            // SOF0 (Start of Frame, baseline DCT) - markers 0xFFC0, 0xFFC1, 0xFFC2
            if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2)
            {
                // Skip marker (0xFF, 0xC0) and length (2 bytes)
                pos += 2;
                pos += 2;

                if (pos + 5 >= data.Length)
                    throw new InvalidOperationException("Invalid JPEG file: Incomplete SOF marker");

                // Extract precision (bits per component)
                info.BitsPerComponent = data[pos];
                pos++;

                // Extract dimensions (big-endian) - HEIGHT comes first, then WIDTH
                info.Height = (data[pos] << 8) | data[pos + 1];
                info.Width = (data[pos + 2] << 8) | data[pos + 3];
                pos += 4;

                // Extract number of components
                info.Components = data[pos];
                pos++;

                // Determine color space from component count
                info.ColorSpace = info.Components switch
                {
                    1 => ImageColorSpace.DeviceGray,
                    3 => ImageColorSpace.DeviceRGB,
                    4 => ImageColorSpace.DeviceCMYK,
                    _ => ImageColorSpace.DeviceRGB // Default to RGB for unknown
                };

                if (info.Width <= 0 || info.Height <= 0)
                    throw new InvalidOperationException("Invalid JPEG file: Invalid dimensions");

                // Don't return yet - keep scanning for APP2/APP14 markers that
                // may appear after SOF in some encoders (rare but legal).
                sofFound = true;
                continue;
            }

            // APP2 (0xFFE2) - may carry one chunk of an embedded ICC profile.
            if (marker == 0xE2)
            {
                int segStart = pos + 2;
                int segLen = (data[segStart] << 8) | data[segStart + 1];
                int payloadStart = segStart + 2;
                int payloadEnd = segStart + segLen;
                if (TryParseIccProfileChunk(data, payloadStart, payloadEnd, out int seq, out int total,
                        out byte[] chunkBytes))
                {
                    iccChunks.Add((seq, chunkBytes));
                    iccTotal = total; // every chunk carries the same total
                }

                pos = payloadEnd;
                continue;
            }

            // APP1 (0xFFE1) - may contain "Exif\0\0" + TIFF header with orientation.
            if (marker == 0xE1)
            {
                int segStart = pos + 2;
                int segLen = (data[segStart] << 8) | data[segStart + 1];
                int payloadStart = segStart + 2;
                int payloadEnd = segStart + segLen;
                TryParseExifOrientation(data, payloadStart, payloadEnd, info);
                pos = payloadEnd;
                continue;
            }

            // APP14 (0xFFEE) - Adobe marker; transform=0 means CMYK ink-inverted.
            if (marker == 0xEE)
            {
                int segStart = pos + 2;
                int segLen = (data[segStart] << 8) | data[segStart + 1];
                int payloadStart = segStart + 2;
                int payloadEnd = segStart + segLen;
                TryParseAdobeMarker(data, payloadStart, payloadEnd, info);
                pos = payloadEnd;
                continue;
            }

            // SOS (Start of Scan) - image data follows, no more metadata
            if (marker == 0xDA)
                break;

            // Skip marker and segment length
            pos += 2;
            if (pos < data.Length - 1)
            {
                int length = (data[pos] << 8) | data[pos + 1];
                pos += length;
            }
        }

        if (!sofFound)
            throw new InvalidOperationException("Could not find JPEG dimensions (missing SOF marker)");

        // Assemble the ICC profile when every announced chunk arrived. If
        // anything is missing (out-of-spec encoder) we drop the profile rather
        // than emit a truncated one - better to render with the device space.
        if (iccChunks.Count > 0 && iccChunks.Count == iccTotal)
        {
            iccChunks.Sort((a, b) => a.Seq.CompareTo(b.Seq));
            int totalLen = 0;
            foreach ((int _, byte[] bytes) in iccChunks) totalLen += bytes.Length;
            byte[] merged = new byte[totalLen];
            int off = 0;
            foreach ((int _, byte[] bytes) in iccChunks)
            {
                Array.Copy(bytes, 0, merged, off, bytes.Length);
                off += bytes.Length;
            }

            info.IccProfile = merged;
        }

        return info;
    }

    /// <summary>
    /// Pulls a single chunk of an embedded ICC profile out of an APP2 payload.
    /// Returns false if the segment isn't an ICC chunk. Each chunk advertises
    /// its 1-based sequence number and the total chunk count so the caller can
    /// reassemble profiles spanning multiple APP2 segments.
    /// </summary>
    private static bool TryParseIccProfileChunk(byte[] data, int start, int end,
        out int seq, out int total, out byte[] bytes)
    {
        seq = 0;
        total = 0;
        bytes = [];

        // "ICC_PROFILE\0" identifier (12 bytes), then seq(1) + total(1) + bytes
        if (end - start < 14 || end > data.Length) return false;
        ReadOnlySpan<byte> id = "ICC_PROFILE\0"u8;
        for (int i = 0; i < id.Length; i++)
            if (data[start + i] != id[i])
                return false;

        seq = data[start + 12];
        total = data[start + 13];
        if (seq < 1 || total < 1 || seq > total) return false;

        bytes = new byte[end - (start + 14)];
        Array.Copy(data, start + 14, bytes, 0, bytes.Length);
        return true;
    }

    /// <summary>
    /// Parses the EXIF orientation tag (0x0112) from an APP1 payload, if
    /// present. Returns silently when the segment isn't an EXIF blob or
    /// doesn't carry an orientation tag - JPEGs without EXIF are very common.
    /// </summary>
    private static void TryParseExifOrientation(byte[] data, int start, int end, ImageInfo info)
    {
        // "Exif\0\0" identifier (6 bytes).
        if (end - start < 6 || end > data.Length) return;
        ReadOnlySpan<byte> exifId = "Exif\0\0"u8;
        for (int i = 0; i < exifId.Length; i++)
            if (data[start + i] != exifId[i])
                return;

        // TIFF header begins immediately after the EXIF identifier. All
        // offsets inside the TIFF block are relative to the TIFF base.
        int tiffBase = start + 6;
        if (end - tiffBase < 8) return;

        // Byte order: "II" = little-endian, "MM" = big-endian.
        bool le = data[tiffBase] == 0x49 && data[tiffBase + 1] == 0x49;
        bool be = data[tiffBase] == 0x4D && data[tiffBase + 1] == 0x4D;
        if (!le && !be) return;

        int U16(int off) => le
            ? data[off] | (data[off + 1] << 8)
            : (data[off] << 8) | data[off + 1];
        long U32(int off) => le
            ? (uint)(data[off] | (data[off + 1] << 8) | (data[off + 2] << 16) | (data[off + 3] << 24))
            : (uint)((data[off] << 24) | (data[off + 1] << 16) | (data[off + 2] << 8) | data[off + 3]);

        if (U16(tiffBase + 2) != 0x002A) return; // TIFF magic

        long ifd0Offset = U32(tiffBase + 4);
        int ifd0 = tiffBase + (int)ifd0Offset;
        if (ifd0 + 2 > end || ifd0 < tiffBase) return;
        int entryCount = U16(ifd0);
        int entry = ifd0 + 2;
        for (int i = 0; i < entryCount; i++, entry += 12)
        {
            if (entry + 12 > end) return;
            int tag = U16(entry);
            if (tag == 0x0112)
            {
                // Orientation tag. Type SHORT (3), count 1, value packed into
                // the first 2 bytes of the 4-byte value field.
                int value = U16(entry + 8);
                if (value is >= 1 and <= 8) info.Orientation = value;
                return;
            }
        }
    }

    /// <summary>
    /// Parses the Adobe APP14 marker. When the transform byte is 0 and the
    /// image is CMYK, ink values are stored inverted (Photoshop convention);
    /// we set the flag so the writer can emit the matching /Decode array.
    /// </summary>
    private static void TryParseAdobeMarker(byte[] data, int start, int end, ImageInfo info)
    {
        // "Adobe\0" identifier is 6 bytes; the segment also carries 5 bytes of
        // version + flags + transform. Total payload >= 12 bytes.
        if (end - start < 12 || end > data.Length) return;
        ReadOnlySpan<byte> id = "Adobe"u8;
        for (int i = 0; i < id.Length; i++)
            if (data[start + i] != id[i])
                return;

        // Bytes: version(2) + flags0(2) + flags1(2) + transform(1).
        // APP14 typically precedes SOF in real-world JPEGs, so we can't check
        // ColorSpace here - the writer guards on CMYK at emit time.
        byte transform = data[start + 11];
        if (transform == 0) info.InvertCmykColors = true;
    }

    /// <summary>
    /// Parse PNG file to extract dimensions, color space, and pixel data
    /// </summary>
    private static ImageInfo ParsePng(byte[] data)
    {
        // Verify PNG signature
        if (data.Length < 8 ||
            data[0] != 0x89 || data[1] != 0x50 || data[2] != 0x4E || data[3] != 0x47 ||
            data[4] != 0x0D || data[5] != 0x0A || data[6] != 0x1A || data[7] != 0x0A)
            throw new InvalidOperationException("Invalid PNG file: Missing PNG signature");

        ImageInfo info = new() { Format = ImageFormat.Png, OriginalData = data };
        List<(int Start, int Length)> idatChunks = new();
        byte[]? palette = null;
        byte[]? compressedIccProfile = null;
        int colorType = 0;
        int bitDepth = 8;

        int pos = 8; // Skip signature

        while (pos < data.Length - 8)
        {
            // Read chunk length (big-endian)
            int chunkLength = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
            pos += 4;

            // Read chunk type
            string chunkType = Encoding.ASCII.GetString(data, pos, 4);
            pos += 4;

            switch (chunkType)
            {
                case "IHDR":
                    // Image header
                    info.Width = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
                    info.Height = (data[pos + 4] << 24) | (data[pos + 5] << 16) | (data[pos + 6] << 8) | data[pos + 7];
                    bitDepth = data[pos + 8];
                    colorType = data[pos + 9];
                    // data[pos + 10] = compression method (always 0)
                    // data[pos + 11] = filter method (always 0)
                    // data[pos + 12] = interlace method (0=none, 1=Adam7)

                    info.BitsPerComponent = bitDepth;

                    // Determine color space and components from color type
                    switch (colorType)
                    {
                        case 0: // Grayscale
                            info.ColorSpace = ImageColorSpace.DeviceGray;
                            info.Components = 1;
                            info.HasAlpha = false;
                            break;
                        case 2: // RGB
                            info.ColorSpace = ImageColorSpace.DeviceRGB;
                            info.Components = 3;
                            info.HasAlpha = false;
                            break;
                        case 3: // Indexed (palette)
                            info.ColorSpace = ImageColorSpace.Indexed;
                            info.Components = 1;
                            info.HasAlpha = false;
                            break;
                        case 4: // Grayscale + Alpha
                            info.ColorSpace = ImageColorSpace.DeviceGray;
                            info.Components = 2;
                            info.HasAlpha = true;
                            break;
                        case 6: // RGBA
                            info.ColorSpace = ImageColorSpace.DeviceRGB;
                            info.Components = 4;
                            info.HasAlpha = true;
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported PNG color type: {colorType}");
                    }

                    break;

                case "PLTE":
                    // Palette chunk
                    palette = new byte[chunkLength];
                    Array.Copy(data, pos, palette, 0, chunkLength);
                    break;

                case "IDAT":
                    // Image data chunk (compressed)
                    idatChunks.Add((pos, chunkLength));
                    break;

                case "iCCP":
                {
                    // Profile name (1-79 bytes, Latin-1, null-terminated) +
                    // compression method (1 byte, always 0) + zlib-compressed
                    // ICC profile. Stash the compressed bytes and inflate them
                    // after the chunk walk.
                    int nameEnd = pos;
                    int limit = Math.Min(pos + chunkLength, pos + 80);
                    while (nameEnd < limit && data[nameEnd] != 0) nameEnd++;
                    // nameEnd points at the terminating null; +1 skips it,
                    // +1 more skips the compression-method byte.
                    int profStart = nameEnd + 2;
                    int profEnd = pos + chunkLength;
                    if (profStart < profEnd)
                    {
                        compressedIccProfile = new byte[profEnd - profStart];
                        Array.Copy(data, profStart, compressedIccProfile, 0, compressedIccProfile.Length);
                    }

                    break;
                }

                case "IEND":
                    // End of image
                    break;
            }

            pos += chunkLength;
            pos += 4; // Skip CRC
        }

        if (info.Width <= 0 || info.Height <= 0)
            throw new InvalidOperationException("Invalid PNG file: Missing or invalid IHDR chunk");

        // The compressed image data is the IDAT chunks end to end. A single chunk is read in place.
        int totalLength = 0;
        foreach ((int _, int length) in idatChunks)
            totalLength += length;

        if (totalLength < 2)
            throw new InvalidOperationException("Failed to decompress PNG data: no image data");

        byte[] compressedData = data;
        int compressedStart, compressedLength;
        if (idatChunks.Count == 1)
        {
            compressedStart = idatChunks[0].Start;
            compressedLength = idatChunks[0].Length;
        }
        else
        {
            compressedData = new byte[totalLength];
            int offset = 0;
            foreach ((int start, int length) in idatChunks)
            {
                Buffer.BlockCopy(data, start, compressedData, offset, length);
                offset += length;
            }

            compressedStart = 0;
            compressedLength = totalLength;
        }

        int bytesPerPixel = info.Components * (bitDepth / 8);
        int scanlineLength = info.Width * bytesPerPixel;
        int stride = scanlineLength + 1; // each row is a filter byte, then the row
        long inflatedLength = (long)stride * info.Height;
        if (inflatedLength > Array.MaxLength)
            throw new InvalidOperationException("Invalid PNG file: image is too large");

        // Inflate into a pooled buffer sized from the header, defilter each row in place, and copy the
        // result out once, so the only allocations are the pixel arrays themselves.
        byte[] inflated = ArrayPool<byte>.Shared.Rent((int)inflatedLength);
        try
        {
            try
            {
                // Skip the 2-byte zlib header.
                using MemoryStream compressedStream = new(compressedData, compressedStart + 2, compressedLength - 2, false);
                using DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress);
                int filled = 0;
                while (filled < (int)inflatedLength)
                {
                    int read = deflateStream.Read(inflated, filled, (int)inflatedLength - filled);
                    if (read == 0) break;
                    filled += read;
                }

                if (filled < (int)inflatedLength)
                    throw new InvalidOperationException("the image data is shorter than the header declares");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to decompress PNG data: {ex.Message}");
            }

            DefilterRows(inflated, info.Height, scanlineLength, bytesPerPixel);

            byte[] pixelData;
            byte[]? alphaData = null;

            if (info.HasAlpha)
            {
                // Separate RGB/Gray and Alpha channels
                int colorComponents = info.Components - 1; // Without alpha
                int colorBytesPerPixel = colorComponents * (bitDepth / 8);
                int alphaBytesPerPixel = bitDepth / 8;

                pixelData = new byte[info.Width * info.Height * colorBytesPerPixel];
                alphaData = new byte[info.Width * info.Height * alphaBytesPerPixel];
                SplitAlpha(inflated, info.Width, info.Height, scanlineLength, bytesPerPixel,
                    colorBytesPerPixel, alphaBytesPerPixel, pixelData, alphaData);

                info.Components = colorComponents;
            }
            else
            {
                // No alpha - just remove filter bytes
                pixelData = new byte[info.Width * info.Height * bytesPerPixel];
                for (int y = 0; y < info.Height; y++)
                    Buffer.BlockCopy(inflated, y * stride + 1, pixelData, y * scanlineLength, scanlineLength);
            }

            info.Data = pixelData;
            info.AlphaData = alphaData ?? Array.Empty<byte>();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inflated);
        }

        info.Palette = palette ?? Array.Empty<byte>();

        // Inflate the iCCP profile if one was carried. The profile stream is
        // zlib-wrapped like IDAT; .NET can inflate it synchronously (the
        // TypeScript implementation needs its async path for this - a
        // platform-mandated difference). A malformed profile is dropped
        // rather than truncated - better to render with the device space.
        if (compressedIccProfile is { Length: > 2 })
        {
            try
            {
                using MemoryStream compressedStream =
                    new(compressedIccProfile, 2, compressedIccProfile.Length - 2);
                using DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress);
                using MemoryStream resultStream = new();
                deflateStream.CopyTo(resultStream);
                info.IccProfile = resultStream.ToArray();
            }
            catch (InvalidDataException)
            {
                // Corrupt profile stream - fall back to the device colour space.
            }
        }

        return info;
    }

    /// <summary>
    /// Undoes the PNG row filters in place. <paramref name="buffer"/> holds each row as a filter byte then
    /// <paramref name="scanlineLength"/> bytes; the Up, Average and Paeth filters read the row above, already
    /// defiltered, and the first row sees zeros.
    /// </summary>
    private static void DefilterRows(byte[] buffer, int height, int scanlineLength, int bytesPerPixel)
    {
        int stride = scanlineLength + 1;
        byte[] zeros = new byte[scanlineLength];

        for (int y = 0; y < height; y++)
        {
            byte filter = buffer[y * stride];
            if (filter == 0) continue;

            Span<byte> row = buffer.AsSpan(y * stride + 1, scanlineLength);
            ReadOnlySpan<byte> above = y == 0 ? zeros : buffer.AsSpan((y - 1) * stride + 1, scanlineLength);
            DefilterScanline(row, above, bytesPerPixel, filter);
        }
    }

    /// <summary>
    /// Splits defiltered rows of interleaved colour-and-alpha samples into a colour plane and an alpha plane.
    /// </summary>
    private static void SplitAlpha(byte[] buffer, int width, int height, int scanlineLength, int bytesPerPixel,
        int colorBytesPerPixel, int alphaBytesPerPixel, byte[] colorOut, byte[] alphaOut)
    {
        int stride = scanlineLength + 1;
        int colorPos = 0;
        int alphaPos = 0;

        for (int y = 0; y < height; y++)
        {
            ReadOnlySpan<byte> row = buffer.AsSpan(y * stride + 1, scanlineLength);

            if (colorBytesPerPixel == 3 && alphaBytesPerPixel == 1)
            {
                PngPixelOperations.SplitRgbaRow(row, colorOut.AsSpan(colorPos, width * 3),
                    alphaOut.AsSpan(alphaPos, width));
                colorPos += width * 3;
                alphaPos += width;
            }
            else if (colorBytesPerPixel == 1 && alphaBytesPerPixel == 1)
            {
                // 8-bit grey with alpha
                for (int x = 0, o = 0; x < width; x++, o += 2)
                {
                    colorOut[colorPos++] = row[o];
                    alphaOut[alphaPos++] = row[o + 1];
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = x * bytesPerPixel;
                    for (int c = 0; c < colorBytesPerPixel; c++)
                        colorOut[colorPos++] = row[pixelOffset + c];
                    for (int a = 0; a < alphaBytesPerPixel; a++)
                        alphaOut[alphaPos++] = row[pixelOffset + colorBytesPerPixel + a];
                }
            }
        }
    }

    /// <summary>
    /// Apply PNG defiltering to a scanline
    /// </summary>
    private static void DefilterScanline(Span<byte> scanline, ReadOnlySpan<byte> prevScanline, int bytesPerPixel, byte filter)
    {
        switch (filter)
        {
            case 0: // None
                break;

            case 1: // Sub
                for (int i = bytesPerPixel; i < scanline.Length; i++)
                    scanline[i] = (byte)(scanline[i] + scanline[i - bytesPerPixel]);
                break;

            case 2: // Up
                for (int i = 0; i < scanline.Length; i++)
                    scanline[i] = (byte)(scanline[i] + prevScanline[i]);
                break;

            case 3: // Average
                for (int i = 0; i < scanline.Length; i++)
                {
                    int a = i >= bytesPerPixel ? scanline[i - bytesPerPixel] : 0;
                    int b = prevScanline[i];
                    scanline[i] = (byte)(scanline[i] + (a + b) / 2);
                }

                break;

            case 4: // Paeth
                for (int i = 0; i < scanline.Length; i++)
                {
                    int a = i >= bytesPerPixel ? scanline[i - bytesPerPixel] : 0;
                    int b = prevScanline[i];
                    int c = i >= bytesPerPixel ? prevScanline[i - bytesPerPixel] : 0;
                    scanline[i] = (byte)(scanline[i] + PaethPredictor(a, b, c));
                }

                break;
        }
    }

    /// <summary>
    /// Paeth predictor function for PNG defiltering
    /// </summary>
    private static int PaethPredictor(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);

        if (pa <= pb && pa <= pc)
            return a;
        if (pb <= pc)
            return b;
        return c;
    }
}
