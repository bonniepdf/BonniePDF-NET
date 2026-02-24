using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Writers;

namespace BonniePDF.Rendering.Images;

/// <summary>
/// Writes image XObjects to PDF
/// Supports both synchronous (IPdfWriter) and asynchronous (IAsyncPdfWriter) operations.
/// </summary>
public class ImageWriter
{
    private readonly IAsyncPdfWriter _asyncWriter;
    private readonly IPdfWriter _writer;

    /// <summary>
    /// When true, PDF/A-1B constraints are enforced (e.g. alpha channels are flattened).
    /// </summary>
    public bool PdfAEnabled { get; set; }

    /// <summary>
    /// Creates an ImageWriter for synchronous operations
    /// </summary>
    public ImageWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    /// <summary>
    /// Creates an ImageWriter for asynchronous operations
    /// </summary>
    public ImageWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    /// <summary>
    /// Writes an image as an XObject, auto-detecting format
    /// </summary>
    /// <returns>The object ID used (may differ if soft mask was created)</returns>
    public int WriteImage(int id, ImageInfo image, string resourceName)
    {
        return image.Format switch
        {
            ImageFormat.Jpeg => WriteJpegImage(id, image, resourceName),
            ImageFormat.Png => WritePngImage(id, image, resourceName),
            _ => throw new InvalidOperationException($"Unsupported image format: {image.Format}")
        };
    }

    /// <summary>
    /// Writes a JPEG image as an XObject
    /// </summary>
    public int WriteJpegImage(int id, ImageInfo image, string resourceName)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/XObject",
            ["/Subtype"] = "/Image",
            ["/Width"] = image.Width.ToString(),
            ["/Height"] = image.Height.ToString(),
            ["/ColorSpace"] = GetColorSpaceString(image.ColorSpace, image.Palette),
            ["/BitsPerComponent"] = image.BitsPerComponent.ToString(),
            ["/Filter"] = "/DCTDecode" // JPEG compression filter
        };

        // Write binary image data
        _writer.WriteBinaryObject(id, dict, image.Data);
        return id;
    }

    /// <summary>
    /// Writes a PNG image as an XObject (using FlateDecode)
    /// </summary>
    public int WritePngImage(int id, ImageInfo image, string resourceName)
    {
        // PDF/A-1B forbids SMask — flatten alpha by compositing against white
        byte[] pixelData = image.Data;
        bool writeSmask = image.HasAlpha && image.AlphaData.Length > 0;

        if (writeSmask && PdfAEnabled)
        {
            pixelData = FlattenAlpha(image.Data, image.AlphaData, image.Components, image.BitsPerComponent);
            writeSmask = false;
        }

        // Compress pixel data using zlib/Deflate
        byte[] compressedData = CompressData(pixelData);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/XObject",
            ["/Subtype"] = "/Image",
            ["/Width"] = image.Width.ToString(),
            ["/Height"] = image.Height.ToString(),
            ["/BitsPerComponent"] = image.BitsPerComponent.ToString(),
            ["/Filter"] = "/FlateDecode"
        };

        // Handle color space
        if (image.ColorSpace == ImageColorSpace.Indexed && image.Palette.Length > 0)
        {
            // Indexed color: [/Indexed /DeviceRGB maxIndex paletteHexString]
            int maxIndex = image.Palette.Length / 3 - 1;
            string paletteHex = "<" + BitConverter.ToString(image.Palette).Replace("-", "") + ">";
            dict["/ColorSpace"] = $"[/Indexed /DeviceRGB {maxIndex} {paletteHex}]";
        }
        else
        {
            dict["/ColorSpace"] = GetColorSpaceString(image.ColorSpace, image.Palette);
        }

        // Handle alpha channel (soft mask)
        if (writeSmask)
        {
            int smaskId = _writer.GetNewId();
            WriteSoftMask(smaskId, image);
            dict["/SMask"] = $"{smaskId} 0 R";
        }

        _writer.WriteBinaryObject(id, dict, compressedData);
        return id;
    }

    /// <summary>
    /// Writes a soft mask (alpha channel) for transparency
    /// </summary>
    private void WriteSoftMask(int id, ImageInfo image)
    {
        byte[] compressedAlpha = CompressData(image.AlphaData);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/XObject",
            ["/Subtype"] = "/Image",
            ["/Width"] = image.Width.ToString(),
            ["/Height"] = image.Height.ToString(),
            ["/ColorSpace"] = "/DeviceGray",
            ["/BitsPerComponent"] = image.BitsPerComponent.ToString(),
            ["/Filter"] = "/FlateDecode"
        };

        _writer.WriteBinaryObject(id, dict, compressedAlpha);
    }

    /// <summary>
    /// Compresses data using zlib/Deflate (Legacy Sync Version)
    /// </summary>
    private byte[] CompressData(byte[] data)
    {
        using MemoryStream output = new();

        // Write zlib header (CMF=78, FLG=9C for default compression)
        output.WriteByte(0x78);
        output.WriteByte(0x9C);

        using (DeflateStream deflate = new(output, CompressionLevel.Optimal, true))
        {
            deflate.Write(data, 0, data.Length);
        }

        // Calculate and write Adler-32 checksum
        uint adler = CalculateAdler32(data);
        output.WriteByte((byte)(adler >> 24));
        output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));
        output.WriteByte((byte)adler);

        return output.ToArray();
    }

    /// <summary>
    /// Compresses data using ArrayPool to avoid allocations (Async Version)
    /// Returns the buffer and the actual length used.
    /// Caller MUST return the buffer to ArrayPool&lt;byte&gt;.Shared
    /// </summary>
    private (byte[] Buffer, int Length) CompressDataToPool(byte[] data)
    {
        // Guess size: Input + overhead. Zlib usually compresses, but can expand slightly if random.
        // We allow some headroom to avoid MemoryStream expansion issues on fixed buffers.
        int capacity = data.Length + data.Length / 100 + 64;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(capacity);

        using (MemoryStream output = new(buffer))
        {
            // Write zlib header
            output.WriteByte(0x78);
            output.WriteByte(0x9C);

            using (DeflateStream deflate = new(output, CompressionLevel.Optimal, true))
            {
                deflate.Write(data, 0, data.Length);
            }

            // Calculate and write Adler-32 checksum
            uint adler = CalculateAdler32(data);
            output.WriteByte((byte)(adler >> 24));
            output.WriteByte((byte)(adler >> 16));
            output.WriteByte((byte)(adler >> 8));
            output.WriteByte((byte)adler);

            return (buffer, (int)output.Position);
        }
    }

    /// <summary>
    /// Calculates Adler-32 checksum for zlib
    /// </summary>
    private uint CalculateAdler32(byte[] data)
    {
        const uint MOD_ADLER = 65521;
        uint a = 1, b = 0;

        foreach (byte d in data)
        {
            a = (a + d) % MOD_ADLER;
            b = (b + a) % MOD_ADLER;
        }

        return (b << 16) | a;
    }

    /// <summary>
    /// Gets the PDF color space string for an image
    /// </summary>
    private string GetColorSpaceString(ImageColorSpace colorSpace, byte[] palette)
    {
        return colorSpace switch
        {
            ImageColorSpace.DeviceRGB => "/DeviceRGB",
            ImageColorSpace.DeviceCMYK => "/DeviceCMYK",
            ImageColorSpace.DeviceGray => "/DeviceGray",
            ImageColorSpace.Indexed => "/DeviceRGB", // Fallback if no palette handling needed
            _ => "/DeviceRGB"
        };
    }

    // ========== ASYNC METHODS ==========

    /// <summary>
    /// Writes an image as an XObject asynchronously, auto-detecting format
    /// </summary>
    public async Task<int> WriteImageAsync(int id, ImageInfo image, string resourceName,
        CancellationToken cancellationToken = default)
    {
        return image.Format switch
        {
            ImageFormat.Jpeg => await WriteJpegImageAsync(id, image, resourceName, cancellationToken)
                .ConfigureAwait(false),
            ImageFormat.Png => await WritePngImageAsync(id, image, resourceName, cancellationToken)
                .ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported image format: {image.Format}")
        };
    }

    /// <summary>
    /// Writes a JPEG image as an XObject asynchronously
    /// </summary>
    public async Task<int> WriteJpegImageAsync(int id, ImageInfo image, string resourceName,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/XObject",
            ["/Subtype"] = "/Image",
            ["/Width"] = image.Width.ToString(),
            ["/Height"] = image.Height.ToString(),
            ["/ColorSpace"] = GetColorSpaceString(image.ColorSpace, image.Palette),
            ["/BitsPerComponent"] = image.BitsPerComponent.ToString(),
            ["/Filter"] = "/DCTDecode"
        };

        // Pass explicit length of the data
        await _asyncWriter.WriteBinaryObjectAsync(id, dict, image.Data, image.Data.Length, cancellationToken)
            .ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Writes a PNG image as an XObject asynchronously (using FlateDecode)
    /// </summary>
    public async Task<int> WritePngImageAsync(int id, ImageInfo image, string resourceName,
        CancellationToken cancellationToken = default)
    {
        // PDF/A-1B forbids SMask — flatten alpha by compositing against white
        byte[] pixelData = image.Data;
        bool writeSmask = image.HasAlpha && image.AlphaData.Length > 0;

        if (writeSmask && PdfAEnabled)
        {
            pixelData = FlattenAlpha(image.Data, image.AlphaData, image.Components, image.BitsPerComponent);
            writeSmask = false;
        }

        // Compress using ArrayPool
        (byte[] compressedBuffer, int compressedLength) = CompressDataToPool(pixelData);

        try
        {
            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Image",
                ["/Width"] = image.Width.ToString(),
                ["/Height"] = image.Height.ToString(),
                ["/BitsPerComponent"] = image.BitsPerComponent.ToString(),
                ["/Filter"] = "/FlateDecode"
            };

            // Handle color space
            if (image.ColorSpace == ImageColorSpace.Indexed && image.Palette.Length > 0)
            {
                int maxIndex = image.Palette.Length / 3 - 1;
                string paletteHex = "<" + BitConverter.ToString(image.Palette).Replace("-", "") + ">";
                dict["/ColorSpace"] = $"[/Indexed /DeviceRGB {maxIndex} {paletteHex}]";
            }
            else
            {
                dict["/ColorSpace"] = GetColorSpaceString(image.ColorSpace, image.Palette);
            }

            // Handle alpha channel (soft mask)
            if (writeSmask)
            {
                int smaskId = _asyncWriter.GetNewId();
                await WriteSoftMaskAsync(smaskId, image, cancellationToken).ConfigureAwait(false);
                dict["/SMask"] = $"{smaskId} 0 R";
            }

            // Write using explicit length
            await _asyncWriter.WriteBinaryObjectAsync(id, dict, compressedBuffer, compressedLength, cancellationToken)
                .ConfigureAwait(false);
            return id;
        }
        finally
        {
            // CRITICAL: Return buffer to pool
            ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    /// <summary>
    /// Writes a soft mask (alpha channel) for transparency asynchronously
    /// </summary>
    private async Task WriteSoftMaskAsync(int id, ImageInfo image, CancellationToken cancellationToken)
    {
        // Compress using ArrayPool
        (byte[] compressedBuffer, int compressedLength) = CompressDataToPool(image.AlphaData);

        try
        {
            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Image",
                ["/Width"] = image.Width.ToString(),
                ["/Height"] = image.Height.ToString(),
                ["/ColorSpace"] = "/DeviceGray",
                ["/BitsPerComponent"] = image.BitsPerComponent.ToString(),
                ["/Filter"] = "/FlateDecode"
            };

            await _asyncWriter.WriteBinaryObjectAsync(id, dict, compressedBuffer, compressedLength, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    /// <summary>
    /// Flattens alpha channel by compositing against white background
    /// </summary>
    private byte[] FlattenAlpha(byte[] data, byte[] alphaData, int components, int bitsPerComponent)
    {
        if (bitsPerComponent != 8)
            throw new NotSupportedException($"Flattening alpha for {bitsPerComponent}-bit images is not supported yet.");

        // Validation
        if (data.Length != alphaData.Length * components)
            throw new ArgumentException("Data length does not match alpha length and component count.");

        byte[] result = new byte[data.Length];
        int pixelCount = alphaData.Length;

        for (int i = 0; i < pixelCount; i++)
        {
            byte alpha = alphaData[i];
            int offset = i * components;

            if (alpha == 255)
            {
                // Opaque - copy directly
                for (int c = 0; c < components; c++)
                {
                    result[offset + c] = data[offset + c];
                }
            }
            else if (alpha == 0)
            {
                // Transparent - set to white (255)
                for (int c = 0; c < components; c++)
                {
                    result[offset + c] = 255;
                }
            }
            else
            {
                // Blend: (Foreground * Alpha + Background * (255 - Alpha)) / 255
                // Background = 255 => 255 * (255 - alpha)
                int invAlpha = 255 - alpha;
                int bgComponent = 255 * invAlpha;

                for (int c = 0; c < components; c++)
                {
                    int fg = data[offset + c];
                    // Integer blend
                    int blended = (fg * alpha + bgComponent) / 255;
                    result[offset + c] = (byte)blended;
                }
            }
        }

        return result;
    }
}