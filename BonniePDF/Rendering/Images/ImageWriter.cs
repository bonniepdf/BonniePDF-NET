using BonniePDF.Rendering.Pdf;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Writers;

namespace BonniePDF;

/// <summary>
/// Writes image XObjects to PDF
/// Supports both synchronous (IPdfWriter) and asynchronous (IAsyncPdfWriter) operations.
/// </summary>
internal class ImageWriter
{
    private readonly IAsyncPdfWriter? _asyncWriter;
    private readonly IPdfWriter? _writer;
    private PdfAConformance _conformance = PdfAConformance.None;

    /// <summary>
    /// Legacy PDF/A switch. Mirrors DocumentSettings: true selects PDF/A-2b.
    /// </summary>
    public bool PdfAEnabled
    {
        get => _conformance != PdfAConformance.None;
        set => _conformance = value ? PdfAConformance.PdfA2b : PdfAConformance.None;
    }

    /// <summary>
    /// PDF/A conformance level used for image feature decisions. PDF/A-1 forbids
    /// transparency, while PDF/A-2 and newer allow soft masks.
    /// </summary>
    public PdfAConformance Conformance
    {
        get => _conformance;
        set => _conformance = value;
    }

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
        // Write binary image data
        _writer!.WriteBinaryObject(id, BuildJpegImageDict(image, ResolveColorSpace(image)), image.Data);
        return id;
    }

    /// <summary>
    /// Builds the XObject dictionary for a JPEG image. Shared by the sync and
    /// async write paths so both emit byte-identical dictionaries.
    /// </summary>
    private static Dictionary<string, string> BuildJpegImageDict(ImageInfo image, string colorSpace)
    {
        Dictionary<string, string> dict = new()
        {
            [PdfKeys.Common.Type] = "/XObject",
            [PdfKeys.Common.Subtype] = "/Image",
            [PdfKeys.Image.Width] = image.Width.ToString(),
            [PdfKeys.Image.Height] = image.Height.ToString(),
            [PdfKeys.Image.ColorSpace] = colorSpace,
            [PdfKeys.Image.BitsPerComponent] = image.BitsPerComponent.ToString(),
            [PdfKeys.Stream.Filter] = "/DCTDecode" // JPEG compression filter
        };

        if (image.InvertCmykColors && image.ColorSpace == ImageColorSpace.DeviceCMYK)
        {
            // Photoshop-exported CMYK JPEGs store inverted ink values; tell PDF
            // to invert each component back during decoding.
            dict[PdfKeys.Image.Decode] = "[1 0 1 0 1 0 1 0]";
        }

        return dict;
    }

    /// <summary>
    /// Writes a PNG image as an XObject (using FlateDecode)
    /// </summary>
    public int WritePngImage(int id, ImageInfo image, string resourceName)
    {
        // PDF/A-1 forbids SMask - flatten alpha by compositing against white.
        byte[] pixelData = image.Data;
        bool writeSmask = image.HasAlpha && image.AlphaData.Length > 0;

        if (writeSmask && RequiresPdfA1TransparencyFallback())
        {
            pixelData = FlattenAlpha(image.Data, image.AlphaData, image.Components, image.BitsPerComponent);
            writeSmask = false;
        }

        // Compress pixel data using zlib/Deflate
        byte[] compressedData = CompressData(pixelData);

        Dictionary<string, string> dict = BuildPngImageDict(image, ResolveColorSpace(image));

        // Handle alpha channel (soft mask)
        if (writeSmask)
        {
            int smaskId = _writer!.GetNewId();
            WriteSoftMask(smaskId, image);
            dict[PdfKeys.Image.SoftMask] = $"{smaskId} 0 R";
        }

        _writer!.WriteBinaryObject(id, dict, compressedData);
        return id;
    }

    /// <summary>
    /// Builds the XObject dictionary for a PNG image. Shared by the sync and
    /// async write paths so both emit byte-identical dictionaries; the
    /// resolved colour space is passed in (its ICC branch writes a stream
    /// object, which differs per path) and the /SMask entry is added by the
    /// caller when a soft mask is written.
    /// </summary>
    private static Dictionary<string, string> BuildPngImageDict(ImageInfo image, string colorSpace)
    {
        return new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/XObject",
            [PdfKeys.Common.Subtype] = "/Image",
            [PdfKeys.Image.Width] = image.Width.ToString(),
            [PdfKeys.Image.Height] = image.Height.ToString(),
            [PdfKeys.Image.BitsPerComponent] = image.BitsPerComponent.ToString(),
            [PdfKeys.Stream.Filter] = "/FlateDecode",
            [PdfKeys.Image.ColorSpace] = colorSpace
        };
    }

    /// <summary>
    /// Returns the value for the image XObject's /ColorSpace entry. When the
    /// image carries an embedded ICC profile we write the profile as its own
    /// stream object and reference it with <c>[/ICCBased id 0 R]</c>; that
    /// gives the consuming reader the captured colour transform instead of
    /// falling back to the device space. Without a profile we fall through to
    /// the indexed palette (for paletted PNGs) or the device colour space.
    /// </summary>
    private string ResolveColorSpace(ImageInfo image)
    {
        string baseDevice = GetColorSpaceString(image.ColorSpace);

        if (ShouldEmbedIccProfile(image))
        {
            int iccId = _writer!.GetNewId();
            WriteIccProfile(iccId, image.IccProfile, image.Components, baseDevice);
            return $"[/ICCBased {iccId} 0 R]";
        }

        return NonIccColorSpace(image, baseDevice);
    }

    /// <summary>
    /// Async sibling of <see cref="ResolveColorSpace"/> - identical decision
    /// logic, but the ICC profile stream is written through the async writer.
    /// </summary>
    private async Task<string> ResolveColorSpaceAsync(ImageInfo image, CancellationToken cancellationToken)
    {
        string baseDevice = GetColorSpaceString(image.ColorSpace);

        if (ShouldEmbedIccProfile(image))
        {
            int iccId = _asyncWriter!.GetNewId();
            await WriteIccProfileAsync(iccId, image.IccProfile, image.Components, baseDevice, cancellationToken)
                .ConfigureAwait(false);
            return $"[/ICCBased {iccId} 0 R]";
        }

        return NonIccColorSpace(image, baseDevice);
    }

    /// <summary>
    /// The component count for the ICC stream's /N entry must match the
    /// decoded image data: the PNG loader splits alpha out before the pixel
    /// stream is written (decrementing Components), and JPEG Components is
    /// already 1, 3, or 4 - so Components is the right value for both.
    /// </summary>
    private static bool ShouldEmbedIccProfile(ImageInfo image) =>
        image.IccProfile.Length > 0 && image.ColorSpace != ImageColorSpace.Indexed && image.Components > 0;

    private static string NonIccColorSpace(ImageInfo image, string baseDevice)
    {
        if (image.ColorSpace == ImageColorSpace.Indexed && image.Palette.Length > 0)
        {
            // Indexed color: [/Indexed /DeviceRGB maxIndex paletteHexString]
            int maxIndex = image.Palette.Length / 3 - 1;
            string paletteHex = "<" + BitConverter.ToString(image.Palette).Replace("-", "") + ">";
            return $"[/Indexed /DeviceRGB {maxIndex} {paletteHex}]";
        }

        return baseDevice;
    }

    /// <summary>
    /// Builds the dictionary for a standalone ICC-profile stream object that
    /// an /ICCBased colour space can point at. /Alternate is set to the
    /// matching device colour space so PDF readers without ICC support fall
    /// back gracefully.
    /// </summary>
    private static Dictionary<string, string> BuildIccProfileDict(int components, string alternate)
    {
        return new Dictionary<string, string>
        {
            [PdfKeys.ColorProfile.Components] = components.ToString(),
            [PdfKeys.ColorProfile.Alternate] = alternate,
            [PdfKeys.Stream.Filter] = "/FlateDecode"
        };
    }

    private void WriteIccProfile(int id, byte[] profile, int components, string alternate)
    {
        byte[] compressed = CompressData(profile);
        _writer!.WriteBinaryObject(id, BuildIccProfileDict(components, alternate), compressed);
    }

    private async Task WriteIccProfileAsync(int id, byte[] profile, int components, string alternate,
        CancellationToken cancellationToken)
    {
        byte[] compressed = CompressData(profile);
        await _asyncWriter!.WriteBinaryObjectAsync(id, BuildIccProfileDict(components, alternate), compressed,
                compressed.Length, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the XObject dictionary for a soft mask (alpha channel).
    /// Shared by the sync and async write paths.
    /// </summary>
    private static Dictionary<string, string> BuildSoftMaskDict(ImageInfo image)
    {
        return new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/XObject",
            [PdfKeys.Common.Subtype] = "/Image",
            [PdfKeys.Image.Width] = image.Width.ToString(),
            [PdfKeys.Image.Height] = image.Height.ToString(),
            [PdfKeys.Image.ColorSpace] = "/DeviceGray",
            [PdfKeys.Image.BitsPerComponent] = image.BitsPerComponent.ToString(),
            [PdfKeys.Stream.Filter] = "/FlateDecode"
        };
    }

    /// <summary>
    /// Writes a soft mask (alpha channel) for transparency
    /// </summary>
    private void WriteSoftMask(int id, ImageInfo image)
    {
        byte[] compressedAlpha = CompressData(image.AlphaData);
        _writer!.WriteBinaryObject(id, BuildSoftMaskDict(image), compressedAlpha);
    }

    /// <summary>The compressed form of one pixel array, kept for as long as the array itself lives.</summary>
    private sealed class CompressedStream
    {
        public ImageCompressionLevel Level;
        public byte[] Bytes = [];
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<byte[], CompressedStream> Compressed = new();

    private static System.IO.Compression.CompressionLevel ToZlibLevel(ImageCompressionLevel level) => level switch
    {
        ImageCompressionLevel.Smallest => CompressionLevel.SmallestSize,
        ImageCompressionLevel.Fast => CompressionLevel.Fastest,
        _ => CompressionLevel.Optimal
    };

    /// <summary>
    /// Compresses data with zlib. The result is remembered against the array, so an image whose pixels are
    /// shared between documents (see <see cref="ImagePipeline"/>) is compressed once, and forgotten with it.
    /// </summary>
    private static byte[] CompressData(byte[] data)
    {
        ImageCompressionLevel level = ImagePipeline.CompressionLevel;
        if (ImagePipeline.CacheMaxBytes <= 0) return Deflate(data, level);

        if (Compressed.TryGetValue(data, out CompressedStream? known) && known.Level == level)
            return known.Bytes;

        CompressedStream made = new() { Level = level, Bytes = Deflate(data, level) };
        Compressed.AddOrUpdate(data, made);
        return made.Bytes;
    }

    private static byte[] Deflate(byte[] data, ImageCompressionLevel level)
    {
        using MemoryStream output = new(Math.Max(256, data.Length / 4));
        using (ZLibStream zlib = new(output, ToZlibLevel(level), true))
        {
            zlib.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Compresses data for the async writer. When the cache is on the shared, remembered stream is returned
    /// (<c>Pooled</c> false: it must not be returned to the pool); otherwise a pooled buffer is filled, which the
    /// caller MUST return to <see cref="ArrayPool{T}.Shared"/>.
    /// </summary>
    private static (byte[] Buffer, int Length, bool Pooled) CompressForAsync(byte[] data)
    {
        if (ImagePipeline.CacheMaxBytes > 0)
        {
            byte[] shared = CompressData(data);
            return (shared, shared.Length, false);
        }

        // Guess size: input + overhead. Zlib may expand incompressible data slightly; the headroom keeps a
        // fixed buffer from overflowing.
        int capacity = data.Length + data.Length / 100 + 64;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(capacity);

        using MemoryStream output = new(buffer);
        using (ZLibStream zlib = new(output, ToZlibLevel(ImagePipeline.CompressionLevel), true))
        {
            zlib.Write(data, 0, data.Length);
        }

        return (buffer, (int)output.Position, true);
    }

    /// <summary>
    /// Gets the PDF color space string for an image
    /// </summary>
    private static string GetColorSpaceString(ImageColorSpace colorSpace)
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
        string colorSpace = await ResolveColorSpaceAsync(image, cancellationToken).ConfigureAwait(false);

        // Pass explicit length of the data
        await _asyncWriter!.WriteBinaryObjectAsync(id, BuildJpegImageDict(image, colorSpace), image.Data,
                image.Data.Length, cancellationToken)
            .ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Writes a PNG image as an XObject asynchronously (using FlateDecode)
    /// </summary>
    public async Task<int> WritePngImageAsync(int id, ImageInfo image, string resourceName,
        CancellationToken cancellationToken = default)
    {
        // PDF/A-1 forbids SMask - flatten alpha by compositing against white.
        byte[] pixelData = image.Data;
        bool writeSmask = image.HasAlpha && image.AlphaData.Length > 0;

        if (writeSmask && RequiresPdfA1TransparencyFallback())
        {
            pixelData = FlattenAlpha(image.Data, image.AlphaData, image.Components, image.BitsPerComponent);
            writeSmask = false;
        }

        // Compress using ArrayPool
        (byte[] compressedBuffer, int compressedLength, bool pooled) = CompressForAsync(pixelData);

        try
        {
            string colorSpace = await ResolveColorSpaceAsync(image, cancellationToken).ConfigureAwait(false);
            Dictionary<string, string> dict = BuildPngImageDict(image, colorSpace);

            // Handle alpha channel (soft mask)
            if (writeSmask)
            {
                int smaskId = _asyncWriter!.GetNewId();
                await WriteSoftMaskAsync(smaskId, image, cancellationToken).ConfigureAwait(false);
                dict[PdfKeys.Image.SoftMask] = $"{smaskId} 0 R";
            }

            // Write using explicit length
            await _asyncWriter!.WriteBinaryObjectAsync(id, dict, compressedBuffer, compressedLength, cancellationToken)
                .ConfigureAwait(false);
            return id;
        }
        finally
        {
            // CRITICAL: Return buffer to pool
            if (pooled) ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    /// <summary>
    /// Writes a soft mask (alpha channel) for transparency asynchronously
    /// </summary>
    private async Task WriteSoftMaskAsync(int id, ImageInfo image, CancellationToken cancellationToken)
    {
        // Compress using ArrayPool
        (byte[] compressedBuffer, int compressedLength, bool pooled) = CompressForAsync(image.AlphaData);

        try
        {
            await _asyncWriter!.WriteBinaryObjectAsync(id, BuildSoftMaskDict(image), compressedBuffer,
                    compressedLength, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (pooled) ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    /// <summary>
    /// Flattens alpha channel by compositing against white background
    /// </summary>
    private static byte[] FlattenAlpha(byte[] data, byte[] alphaData, int components, int bitsPerComponent)
    {
        if (bitsPerComponent != 8)
            throw new NotSupportedException($"Flattening alpha for {bitsPerComponent}-bit images is not supported yet.");

        // Validation
        if (data.Length != alphaData.Length * components)
            throw new ArgumentException("Data length does not match alpha length and component count.", nameof(data));

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

    private bool RequiresPdfA1TransparencyFallback() =>
        _conformance is PdfAConformance.PdfA1b or PdfAConformance.PdfA1a;
}
