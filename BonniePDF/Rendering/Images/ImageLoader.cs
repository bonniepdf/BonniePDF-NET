using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BonniePDF.Rendering.Images;

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

        // Parse JPEG segments to find SOF0 (Start of Frame) marker
        int pos = 2;

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
                int length = (data[pos] << 8) | data[pos + 1];
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

                return info;
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

        throw new InvalidOperationException("Could not find JPEG dimensions (missing SOF marker)");
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
        List<byte[]> idatChunks = new();
        byte[]? palette = null;
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
                    byte[] chunk = new byte[chunkLength];
                    Array.Copy(data, pos, chunk, 0, chunkLength);
                    idatChunks.Add(chunk);
                    break;

                case "IEND":
                    // End of image
                    break;
            }

            pos += chunkLength;
            pos += 4; // Skip CRC
        }

        if (info.Width <= 0 || info.Height <= 0)
            throw new InvalidOperationException("Invalid PNG file: Missing or invalid IHDR chunk");

        // Combine all IDAT chunks
        int totalLength = 0;
        foreach (byte[] chunk in idatChunks)
            totalLength += chunk.Length;

        byte[] compressedData = new byte[totalLength];
        int offset = 0;
        foreach (byte[] chunk in idatChunks)
        {
            Array.Copy(chunk, 0, compressedData, offset, chunk.Length);
            offset += chunk.Length;
        }

        // Decompress the image data using zlib (skip the 2-byte zlib header)
        byte[] decompressedData;
        try
        {
            using MemoryStream compressedStream = new(compressedData, 2, compressedData.Length - 2);
            using DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress);
            using MemoryStream resultStream = new();
            deflateStream.CopyTo(resultStream);
            decompressedData = resultStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decompress PNG data: {ex.Message}");
        }

        // Remove PNG filter bytes and extract pixel data
        int bytesPerPixel = info.Components * (bitDepth / 8);
        int scanlineLength = info.Width * bytesPerPixel;
        int rawDataLength = info.Width * info.Height * bytesPerPixel;

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

            int srcPos = 0;
            int colorPos = 0;
            int alphaPos = 0;

            for (int y = 0; y < info.Height; y++)
            {
                // Skip filter byte
                byte filter = decompressedData[srcPos++];
                byte[] prevScanline = new byte[scanlineLength];
                byte[] currScanline = new byte[scanlineLength];

                // Read and defilter the scanline
                Array.Copy(decompressedData, srcPos, currScanline, 0, scanlineLength);
                DefilterScanline(currScanline, prevScanline, bytesPerPixel, filter);

                // Separate color and alpha
                for (int x = 0; x < info.Width; x++)
                {
                    int pixelOffset = x * bytesPerPixel;

                    // Copy color components
                    for (int c = 0; c < colorBytesPerPixel; c++)
                        pixelData[colorPos++] = currScanline[pixelOffset + c];

                    // Copy alpha
                    for (int a = 0; a < alphaBytesPerPixel; a++)
                        alphaData[alphaPos++] = currScanline[pixelOffset + colorBytesPerPixel + a];
                }

                srcPos += scanlineLength;
                Array.Copy(currScanline, prevScanline, scanlineLength);
            }

            info.Components = colorComponents;
        }
        else
        {
            // No alpha - just remove filter bytes
            pixelData = new byte[rawDataLength];
            int srcPos = 0;
            int dstPos = 0;
            byte[] prevScanline = new byte[scanlineLength];

            for (int y = 0; y < info.Height; y++)
            {
                byte filter = decompressedData[srcPos++];
                byte[] currScanline = new byte[scanlineLength];
                Array.Copy(decompressedData, srcPos, currScanline, 0, scanlineLength);

                DefilterScanline(currScanline, prevScanline, bytesPerPixel, filter);

                Array.Copy(currScanline, 0, pixelData, dstPos, scanlineLength);

                srcPos += scanlineLength;
                dstPos += scanlineLength;
                Array.Copy(currScanline, prevScanline, scanlineLength);
            }
        }

        info.Data = pixelData;
        info.AlphaData = alphaData ?? Array.Empty<byte>();
        info.Palette = palette ?? Array.Empty<byte>();

        return info;
    }

    /// <summary>
    /// Apply PNG defiltering to a scanline
    /// </summary>
    private static void DefilterScanline(byte[] scanline, byte[] prevScanline, int bytesPerPixel, byte filter)
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