using BonniePDF.Rendering.Images;
using Xunit;

namespace BonniePDF.Tests.Drawing;

public class ImageLoaderTests
{
    private const string TestImagePath = @"BonniePDF.Examples\img\owl.jpg";

    // Helper: Create a minimal valid JPEG byte array
    private static byte[] CreateMinimalJpeg()
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
}
