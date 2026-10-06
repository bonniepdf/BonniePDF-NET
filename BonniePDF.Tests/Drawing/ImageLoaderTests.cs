using Xunit;

namespace BonniePDF.Tests.Drawing;

public class ImageLoaderTests
{
    private const string TestImagePath = @"BonniePDF.Examples\img\owl.jpg";

    // Helper: Create a minimal valid JPEG byte array
    internal static byte[] CreateMinimalJpeg()
    {
        // Minimal JPEG: SOI (FFD8) + SOF0 marker with dimensions + EOI (FFD9)
        return new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xC0, // SOF0
            0x00, 0x11, // Length (17 bytes)
            0x08, // Precision (8 bits)
            0x00, 0x10, // Height (16)
            0x00, 0x20, // Width (32)
            0x03, // Components (3 = RGB)
            0x01, 0x11, 0x00, // Component 1
            0x02, 0x11, 0x00, // Component 2
            0x03, 0x11, 0x00, // Component 3
            0xFF, 0xD9  // EOI
        };
    }

    // Helper: Load the real test PNG file
    private static byte[] LoadTestPng()
    {
        string pngPath = Path.Combine(AppContext.BaseDirectory, "image.png");
        if (!File.Exists(pngPath))
        {
            throw new FileNotFoundException($"Test PNG file not found at: {pngPath}");
        }
        return File.ReadAllBytes(pngPath);
    }

    [Fact]
    public void Load_FromFilePath_WithJpeg_LoadsSuccessfully()
    {
        if (!File.Exists(TestImagePath))
        {
            // Skip test if image doesn't exist
            return;
        }

        var image = ImageLoader.Load(TestImagePath);

        Assert.NotNull(image);
        Assert.Equal(ImageFormat.Jpeg, image.Format);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.NotNull(image.Data);
        Assert.True(image.Data.Length > 0);
    }

    [Fact]
    public void Load_FromNonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => ImageLoader.Load("nonexistent.jpg"));
    }

    [Fact]
    public void Load_FromByteArray_WithJpeg_LoadsSuccessfully()
    {
        byte[] jpegData = CreateMinimalJpeg();
        var image = ImageLoader.Load(jpegData);

        Assert.NotNull(image);
        Assert.Equal(ImageFormat.Jpeg, image.Format);
        Assert.Equal(32, image.Width);
        Assert.Equal(16, image.Height);
        Assert.Equal(3, image.Components); // RGB
        Assert.Equal(ImageColorSpace.DeviceRGB, image.ColorSpace);
    }

    [Fact]
    public void Load_FromByteArray_WithPng_LoadsSuccessfully()
    {
        byte[] pngData = LoadTestPng();
        var image = ImageLoader.Load(pngData);

        Assert.NotNull(image);
        Assert.Equal(ImageFormat.Png, image.Format);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.True(image.Components > 0);
    }

    [Fact]
    public void Load_WithNullData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ImageLoader.Load((byte[])null));
    }

    [Fact]
    public void Load_WithEmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ImageLoader.Load(Array.Empty<byte>()));
    }

    [Fact]
    public void Load_WithTooSmallData_ThrowsArgumentException()
    {
        byte[] tooSmall = new byte[] { 0x00, 0x01, 0x02 };
        Assert.Throws<ArgumentException>(() => ImageLoader.Load(tooSmall));
    }

    [Fact]
    public void Load_WithUnknownFormat_ThrowsInvalidOperationException()
    {
        byte[] unknownData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.Throws<InvalidOperationException>(() => ImageLoader.Load(unknownData));
    }

    [Fact]
    public void LoadJpeg_WithValidData_ParsesCorrectly()
    {
        byte[] jpegData = CreateMinimalJpeg();
        var image = ImageLoader.LoadJpeg(jpegData);

        Assert.Equal(ImageFormat.Jpeg, image.Format);
        Assert.Equal(32, image.Width);
        Assert.Equal(16, image.Height);
        Assert.Equal(8, image.BitsPerComponent);
        Assert.Equal(3, image.Components);
        Assert.Equal(ImageColorSpace.DeviceRGB, image.ColorSpace);
    }

    [Fact]
    public void LoadJpeg_PreservesOriginalData()
    {
        byte[] jpegData = CreateMinimalJpeg();
        var image = ImageLoader.LoadJpeg(jpegData);

        Assert.Equal(jpegData, image.Data);
        Assert.Equal(jpegData, image.OriginalData);
    }

    [Fact]
    public void LoadJpeg_WithGrayscale_ParsesCorrectly()
    {
        // Create grayscale JPEG (1 component)
        byte[] grayscaleJpeg = new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xC0, // SOF0
            0x00, 0x0B, // Length (11 bytes)
            0x08, // Precision
            0x00, 0x10, // Height
            0x00, 0x20, // Width
            0x01, // Components (1 = Grayscale)
            0x01, 0x11, 0x00, // Component 1
            0xFF, 0xD9  // EOI
        };

        var image = ImageLoader.LoadJpeg(grayscaleJpeg);

        Assert.Equal(1, image.Components);
        Assert.Equal(ImageColorSpace.DeviceGray, image.ColorSpace);
    }

    [Fact]
    public void LoadJpeg_WithCMYK_ParsesCorrectly()
    {
        // Create CMYK JPEG (4 components)
        byte[] cmykJpeg = new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xC0, // SOF0
            0x00, 0x14, // Length (20 bytes)
            0x08, // Precision
            0x00, 0x10, // Height
            0x00, 0x20, // Width
            0x04, // Components (4 = CMYK)
            0x01, 0x11, 0x00,
            0x02, 0x11, 0x00,
            0x03, 0x11, 0x00,
            0x04, 0x11, 0x00,
            0xFF, 0xD9  // EOI
        };

        var image = ImageLoader.LoadJpeg(cmykJpeg);

        Assert.Equal(4, image.Components);
        Assert.Equal(ImageColorSpace.DeviceCMYK, image.ColorSpace);
    }

    [Fact]
    public void LoadJpeg_WithMissingSignature_ThrowsInvalidOperationException()
    {
        byte[] invalidJpeg = new byte[] { 0x00, 0x00, 0xFF, 0xD8, 0xFF };
        Assert.Throws<InvalidOperationException>(() => ImageLoader.LoadJpeg(invalidJpeg));
    }

    [Fact]
    public void LoadJpeg_WithMissingSOFMarker_ThrowsInvalidOperationException()
    {
        byte[] noSOF = new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xDA, // SOS (Start of Scan) - no SOF before it
            0x00, 0x00
        };

        Assert.Throws<InvalidOperationException>(() => ImageLoader.LoadJpeg(noSOF));
    }

    [Fact]
    public void LoadPng_WithValidData_ParsesCorrectly()
    {
        byte[] pngData = LoadTestPng();
        var image = ImageLoader.LoadPng(pngData);

        Assert.Equal(ImageFormat.Png, image.Format);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.Equal(8, image.BitsPerComponent);
        Assert.True(image.Components > 0);
    }

    [Fact]
    public void LoadPng_WithMissingSignature_ThrowsInvalidOperationException()
    {
        byte[] invalidPng = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.Throws<InvalidOperationException>(() => ImageLoader.LoadPng(invalidPng));
    }

    [Fact]
    public void LoadPng_DecompressesPixelData()
    {
        byte[] pngData = LoadTestPng();
        var image = ImageLoader.LoadPng(pngData);

        // For PNG, pixel data should be decompressed
        Assert.NotNull(image.Data);
        Assert.True(image.Data.Length > 0);

        // Should have width * height * components bytes
        int expectedSize = image.Width * image.Height * image.Components;
        Assert.Equal(expectedSize, image.Data.Length);
    }

    // A deterministic RGBA pattern with real variation in both axes, so the Up,
    // Average and Paeth filters (which read the row above) can't decode correctly
    // by accident.
    internal static byte Sample(int x, int y, int channel) =>
        (byte)((x * 37 + y * 91 + channel * 53 + (x * y) % 29) & 0xFF);

    // Encodes a Width x Height RGBA PNG, filtering row y with filterOf(y). Written
    // independently of the loader: it applies the forward PNG filters per the spec.
    internal static byte[] BuildFilteredRgbaPng(int width, int height, Func<int, byte> filterOf)
    {
        const int bpp = 4;
        int rowBytes = width * bpp;
        byte[] prev = new byte[rowBytes];
        var raw = new List<byte>();

        for (int y = 0; y < height; y++)
        {
            byte[] cur = new byte[rowBytes];
            for (int x = 0; x < width; x++)
                for (int c = 0; c < bpp; c++)
                    cur[x * bpp + c] = Sample(x, y, c);

            byte filter = filterOf(y);
            raw.Add(filter);
            for (int i = 0; i < rowBytes; i++)
            {
                int a = i >= bpp ? cur[i - bpp] : 0;
                int b = prev[i];
                int cc = i >= bpp ? prev[i - bpp] : 0;
                int predictor = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    _ => Paeth(a, b, cc),
                };
                raw.Add((byte)(cur[i] - predictor));
            }
            prev = cur;
        }

        using var zlib = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(zlib, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            z.Write(raw.ToArray());

        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        void Chunk(string type, byte[] data)
        {
            byte[] len = BitConverter.GetBytes(data.Length); Array.Reverse(len);
            png.Write(len);
            byte[] body = System.Text.Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
            png.Write(body);
            uint crc = 0xFFFFFFFF;
            foreach (byte v in body)
            {
                crc ^= v;
                for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
            byte[] crcBytes = BitConverter.GetBytes(~crc); Array.Reverse(crcBytes);
            png.Write(crcBytes);
        }

        byte[] w = BitConverter.GetBytes(width); Array.Reverse(w);
        byte[] h = BitConverter.GetBytes(height); Array.Reverse(h);
        Chunk("IHDR", w.Concat(h).Concat(new byte[] { 8, 6, 0, 0, 0 }).ToArray());
        Chunk("IDAT", zlib.ToArray());
        Chunk("IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    [Theory]
    [InlineData(0, "None")]
    [InlineData(1, "Sub")]
    [InlineData(2, "Up")]
    [InlineData(3, "Average")]
    [InlineData(4, "Paeth")]
    [InlineData(5, "rows cycling through every filter")]
    public void LoadPng_Rgba_DefiltersEveryFilterType(int filter, string description)
    {
        _ = description;
        const int width = 23, height = 17;
        byte[] png = BuildFilteredRgbaPng(width, height, y => (byte)(filter == 5 ? y % 5 : filter));

        var image = ImageLoader.LoadPng(png);

        Assert.True(image.HasAlpha);
        Assert.Equal(3, image.Components);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixel = y * width + x;
                for (int c = 0; c < 3; c++)
                    Assert.Equal(Sample(x, y, c), image.Data[pixel * 3 + c]);
                Assert.Equal(Sample(x, y, 3), image.AlphaData[pixel]);
            }
        }
    }

    // Note: Tests for grayscale, RGBA, indexed color, and alpha channel separation
    // would require specific test PNG files for each format. The current test PNG
    // is used for basic PNG loading validation.

    [Fact]
    public void LoadPng_WithoutAlpha_HasEmptyAlphaData()
    {
        byte[] pngData = LoadTestPng();
        var image = ImageLoader.LoadPng(pngData);

        Assert.NotNull(image.AlphaData);
        // The test PNG may or may not have alpha, so we just verify AlphaData is not null
        // If it has no alpha, AlphaData should be empty
        if (!image.HasAlpha)
        {
            Assert.Empty(image.AlphaData);
        }
    }

    [Fact]
    public void LoadPng_StoresOriginalData()
    {
        byte[] pngData = LoadTestPng();
        var image = ImageLoader.LoadPng(pngData);

        Assert.Equal(pngData, image.OriginalData);
    }

    [Fact]
    public void LoadPng_WithInvalidDimensions_ThrowsInvalidOperationException()
    {
        // Create a minimal invalid PNG with zero dimensions
        byte[] invalidPng = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR length
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x00, // Width = 0 (invalid)
            0x00, 0x00, 0x00, 0x00, // Height = 0 (invalid)
            0x08, 0x02, 0x00, 0x00, 0x00 // bit depth, color type, etc.
        };

        Assert.Throws<InvalidOperationException>(() => ImageLoader.LoadPng(invalidPng));
    }

    [Fact]
    public void LoadJpeg_WithInvalidDimensions_ThrowsInvalidOperationException()
    {
        byte[] invalidJpeg = new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xC0, // SOF0
            0x00, 0x11, // Length
            0x08, // Precision
            0x00, 0x00, // Height (0 - invalid)
            0x00, 0x00, // Width (0 - invalid)
            0x03, // Components
            0x01, 0x11, 0x00,
            0x02, 0x11, 0x00,
            0x03, 0x11, 0x00,
            0xFF, 0xD9
        };

        Assert.Throws<InvalidOperationException>(() => ImageLoader.LoadJpeg(invalidJpeg));
    }

    // 32x16 baseline RGB SOF0 segment, shared by the ICC / Adobe helpers below.
    private static readonly byte[] RgbSof0 =
    [
        0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03,
        0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00
    ];

    // Build a 32x16 baseline JPEG with an APP14 Adobe marker carrying the
    // given transform byte, and 4 component channels (CMYK).
    private static byte[] CreateCmykJpegWithAdobeMarker(byte transform)
    {
        // APP14 payload: "Adobe" + version(2) + flags0(2) + flags1(2) + transform(1) = 12 bytes
        byte[] app14Payload = [0x41, 0x64, 0x6F, 0x62, 0x65, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, transform];
        int app14Len = app14Payload.Length + 2;

        // SOF0 with 4 components -> CMYK
        byte[] sof0 =
        [
            0xFF, 0xC0, 0x00, 0x14, 0x08, 0x00, 0x10, 0x00, 0x20, 0x04,
            0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00, 0x04, 0x11, 0x00
        ];

        List<byte> bytes = [0xFF, 0xD8, 0xFF, 0xEE, (byte)(app14Len >> 8), (byte)(app14Len & 0xFF)];
        bytes.AddRange(app14Payload);
        bytes.AddRange(sof0);
        bytes.AddRange([0xFF, 0xD9]);
        return bytes.ToArray();
    }

    // Build a 32x16 RGB JPEG carrying a synthetic ICC profile spread across
    // the given chunks. Sequence numbers default to 1..chunks.Length; pass
    // seqOrder to write them in a different order (the loader should sort).
    // announceTotal defaults to chunks.Length; pass a larger number to
    // simulate a truncated profile (the loader should then drop it).
    private static byte[] CreateJpegWithIccProfile(byte[][] chunks, int[] seqOrder = null, int? announceTotal = null)
    {
        int total = announceTotal ?? chunks.Length;
        byte[] iccId = "ICC_PROFILE\0"u8.ToArray();

        List<byte> bytes = [0xFF, 0xD8];
        for (int i = 0; i < chunks.Length; i++)
        {
            int seq = seqOrder?[i] ?? i + 1;
            int segLen = iccId.Length + 2 + chunks[i].Length + 2;
            bytes.AddRange([0xFF, 0xE2, (byte)(segLen >> 8), (byte)(segLen & 0xFF)]);
            bytes.AddRange(iccId);
            bytes.Add((byte)seq);
            bytes.Add((byte)total);
            bytes.AddRange(chunks[i]);
        }

        bytes.AddRange(RgbSof0);
        bytes.AddRange([0xFF, 0xD9]);
        return bytes.ToArray();
    }

    private static void AppendPngChunk(List<byte> bytes, string type, byte[] payload)
    {
        bytes.AddRange([
            (byte)(payload.Length >> 24), (byte)(payload.Length >> 16),
            (byte)(payload.Length >> 8), (byte)(payload.Length & 0xFF)
        ]);
        bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(type));
        bytes.AddRange(payload);
        bytes.AddRange([0x00, 0x00, 0x00, 0x00]); // CRC (unchecked by the loader)
    }

    // Build a minimal 1x1 RGB PNG carrying the given ICC profile in an iCCP chunk.
    private static byte[] CreatePngWithIccp(byte[] profile)
    {
        // 1x1 RGB image: one scanline = filter byte + 3 colour bytes.
        byte[] raw = [0, 0, 0, 0];
        byte[] idat = BuildStoredZlib(raw);

        List<byte> iccp = [(byte)'i', (byte)'c', (byte)'c', 0x00, 0x00]; // name "icc" + null + method 0
        iccp.AddRange(BuildStoredZlib(profile));

        List<byte> bytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        AppendPngChunk(bytes, "IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 2, 0, 0, 0]);
        AppendPngChunk(bytes, "iCCP", iccp.ToArray());
        AppendPngChunk(bytes, "IDAT", idat);
        AppendPngChunk(bytes, "IEND", []);
        return bytes.ToArray();
    }

    // Wrap bytes in a zlib stream using a single stored (uncompressed) deflate block.
    private static byte[] BuildStoredZlib(byte[] raw)
    {
        int len = raw.Length;
        int nlen = ~len & 0xFFFF;
        List<byte> zlib =
        [
            0x78, 0x01, // zlib header
            0x01, // BFINAL=1, BTYPE=00 (stored)
            (byte)(len & 0xFF), (byte)(len >> 8),
            (byte)(nlen & 0xFF), (byte)(nlen >> 8)
        ];
        zlib.AddRange(raw);
        uint adler = PngAdler32(raw);
        zlib.AddRange([(byte)(adler >> 24), (byte)(adler >> 16), (byte)(adler >> 8), (byte)(adler & 0xFF)]);
        return zlib.ToArray();
    }

    private static uint PngAdler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1, b = 0;
        foreach (byte d in data)
        {
            a = (a + d) % mod;
            b = (b + a) % mod;
        }

        return (b << 16) | a;
    }

    [Fact]
    public void LoadJpeg_AdobeMarkerTransformZero_SetsInvertCmykColors()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateCmykJpegWithAdobeMarker(transform: 0));

        Assert.Equal(ImageColorSpace.DeviceCMYK, info.ColorSpace);
        Assert.True(info.InvertCmykColors);
    }

    [Fact]
    public void LoadJpeg_AdobeMarkerTransformNonZero_DoesNotSetInvertCmykColors()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateCmykJpegWithAdobeMarker(transform: 2));

        Assert.Equal(ImageColorSpace.DeviceCMYK, info.ColorSpace);
        Assert.False(info.InvertCmykColors);
    }

    [Fact]
    public void LoadJpeg_SingleChunkIccProfile_AssemblesProfile()
    {
        byte[] fakeProfile = new byte[100];
        for (int i = 0; i < fakeProfile.Length; i++) fakeProfile[i] = (byte)i;

        ImageInfo info = ImageLoader.LoadJpeg(CreateJpegWithIccProfile([fakeProfile]));

        Assert.Equal(100, info.IccProfile.Length);
        Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, info.IccProfile.Take(5).ToArray());
    }

    [Fact]
    public void LoadJpeg_MultiChunkIccProfile_AssemblesInSequenceOrder()
    {
        byte[] chunkA = [10, 11, 12];
        byte[] chunkB = [20, 21, 22];
        byte[] chunkC = [30, 31, 32];

        // Provide chunks out of order to verify the loader sorts by sequence number.
        ImageInfo info = ImageLoader.LoadJpeg(CreateJpegWithIccProfile([chunkB, chunkC, chunkA], [2, 3, 1]));

        Assert.Equal(new byte[] { 10, 11, 12, 20, 21, 22, 30, 31, 32 }, info.IccProfile);
    }

    [Fact]
    public void LoadJpeg_TruncatedIccProfile_DropsProfile()
    {
        ImageInfo info =
            ImageLoader.LoadJpeg(CreateJpegWithIccProfile([[1, 2]], [1], announceTotal: 2));

        Assert.Empty(info.IccProfile);
    }

    [Fact]
    public void LoadJpeg_WithoutIccProfile_LeavesProfileEmpty()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateMinimalJpeg());

        Assert.Empty(info.IccProfile);
        Assert.False(info.InvertCmykColors);
    }

    [Fact]
    public void LoadPng_WithIccpChunk_InflatesAndExposesProfile()
    {
        byte[] profile = [10, 20, 30, 40, 50, 60];

        ImageInfo info = ImageLoader.LoadPng(CreatePngWithIccp(profile));

        Assert.Equal(profile, info.IccProfile);
    }

    // Build a 32x16 baseline RGB JPEG carrying an APP1 EXIF segment with an
    // orientation tag (TIFF tag 0x0112).
    private static byte[] CreateJpegWithExifOrientation(int orientation, bool bigEndian = false)
    {
        byte[] U16(int n) => bigEndian
            ? [(byte)((n >> 8) & 0xFF), (byte)(n & 0xFF)]
            : [(byte)(n & 0xFF), (byte)((n >> 8) & 0xFF)];
        byte[] U32(int n) => bigEndian
            ? [(byte)((n >> 24) & 0xFF), (byte)((n >> 16) & 0xFF), (byte)((n >> 8) & 0xFF), (byte)(n & 0xFF)]
            : [(byte)(n & 0xFF), (byte)((n >> 8) & 0xFF), (byte)((n >> 16) & 0xFF), (byte)((n >> 24) & 0xFF)];

        List<byte> tiff = [];
        tiff.AddRange(bigEndian ? [0x4D, 0x4D] : [0x49, 0x49]); // byte order
        tiff.AddRange(U16(0x002A)); // TIFF magic
        tiff.AddRange(U32(8)); // IFD0 offset
        tiff.AddRange(U16(1)); // entry count
        tiff.AddRange(U16(0x0112)); // tag = Orientation
        tiff.AddRange(U16(3)); // type = SHORT
        tiff.AddRange(U32(1)); // count
        tiff.AddRange(U16(orientation)); // packed value
        tiff.AddRange([0x00, 0x00]); // padding for the 4-byte value field
        tiff.AddRange(U32(0)); // next IFD = 0

        List<byte> app1Payload = [0x45, 0x78, 0x69, 0x66, 0x00, 0x00]; // "Exif\0\0"
        app1Payload.AddRange(tiff);
        int app1Len = app1Payload.Count + 2;

        // SOF0: length 17, precision 8, 16x32 (h x w), 3 components.
        byte[] sof0 =
        [
            0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03,
            0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00
        ];

        List<byte> bytes = [0xFF, 0xD8, 0xFF, 0xE1, (byte)(app1Len >> 8), (byte)(app1Len & 0xFF)];
        bytes.AddRange(app1Payload);
        bytes.AddRange(sof0);
        bytes.AddRange([0xFF, 0xD9]);
        return bytes.ToArray();
    }

    [Fact]
    public void LoadJpeg_WithoutExif_DefaultsToOrientationOne()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateMinimalJpeg());

        Assert.Equal(1, info.Orientation);
        Assert.Equal(info.Width, info.DisplayWidth);
        Assert.Equal(info.Height, info.DisplayHeight);
    }

    [Fact]
    public void LoadJpeg_ExifOrientation6_SwapsDisplayDimensions()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateJpegWithExifOrientation(6));

        Assert.Equal(6, info.Orientation);
        // 32x16 stored; orientation 6 -> displayed 16x32.
        Assert.Equal(16, info.DisplayWidth);
        Assert.Equal(32, info.DisplayHeight);
    }

    [Fact]
    public void LoadJpeg_ExifOrientation3_BigEndianTiff_DoesNotSwapAspect()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateJpegWithExifOrientation(3, bigEndian: true));

        Assert.Equal(3, info.Orientation);
        Assert.Equal(info.Width, info.DisplayWidth); // 180 degrees doesn't swap aspect
    }

    [Fact]
    public void LoadJpeg_InvalidExifOrientation_IgnoresValue()
    {
        ImageInfo info = ImageLoader.LoadJpeg(CreateJpegWithExifOrientation(99));

        Assert.Equal(1, info.Orientation);
    }
}
