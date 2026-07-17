using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Writers;
using Xunit;

namespace BonniePDF.Tests.Drawing;

public class ImageWriterTests
{
    // Mock implementation of IPdfWriter for testing
    private sealed class MockPdfWriter : IPdfWriter
    {
        public List<(int id, IDictionary<string, string> dict, byte[] data)> WrittenObjects { get; } = new();
        private int _nextId = 2; // Start from 2 to avoid conflicts with test-provided IDs

        public BonniePDF.Security.Encryption.PdfEncryptor Encryptor { get; set; }

        public int GetNewId() => _nextId++;

        public void WriteBinaryObject(int id, IDictionary<string, string> dict, byte[] data)
        {
            WrittenObjects.Add((id, dict, data));
        }

        public void WriteObject(int id, IDictionary<string, string> dict, string streamContent = null)
        {
            throw new NotImplementedException();
        }

        public void WriteRawObject(int id, string body) { }

        public void WriteCompressedObject(int id, IDictionary<string, string> dict, string content)
        {
            throw new NotImplementedException();
        }

        public void WriteCompressedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent)
        {
            throw new NotImplementedException();
        }

        public int WriteLinkAnnotation(int id, Hyperlink hyperlink, IList<int> pageIds = null, int? structParentKey = null)
        {
            throw new NotImplementedException();
        }

        public int WriteDocumentInfo(int id, DocumentMetadata metadata)
        {
            throw new NotImplementedException();
        }

        public int WriteViewerPreferences(int id, BonniePDF.Configuration.DocumentSettings settings)
        {
            throw new NotImplementedException();
        }

        public void WriteCatalog(int id, int pagesId, int firstPageId, BonniePDF.Configuration.DocumentSettings settings, CatalogOptions options)
        {
            throw new NotImplementedException();
        }

        public void WriteUnencryptedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent)
        {
            throw new NotImplementedException();
        }

        public void Close(int rootId, int infoId = 0)
        {
            throw new NotImplementedException();
        }

        public int WriteEncryptDictionary(int id)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void Constructor_WithValidWriter_CreatesImageWriter()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        Assert.NotNull(imageWriter);
    }

    [Fact]
    public void Constructor_WithNullWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ImageWriter((IPdfWriter)null));
    }

    [Fact]
    public void WriteImage_WithJpeg_CallsWriteJpegImage()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var jpegImage = new ImageInfo
        {
            Format = ImageFormat.Jpeg,
            Width = 100,
            Height = 50,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }
        };

        int result = imageWriter.WriteImage(1, jpegImage, "/Im1");

        Assert.Equal(1, result);
        Assert.Single(mockWriter.WrittenObjects);

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal(1, written.id);
        Assert.Equal("/XObject", written.dict["/Type"]);
        Assert.Equal("/Image", written.dict["/Subtype"]);
        Assert.Equal("/DCTDecode", written.dict["/Filter"]);
    }

    [Fact]
    public void WriteImage_WithPng_CallsWritePngImage()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 100,
            Height = 50,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[] { 0x00, 0x01, 0x02, 0x03 },
            AlphaData = Array.Empty<byte>()
        };

        int result = imageWriter.WriteImage(2, pngImage, "/Im2");

        Assert.Equal(2, result);
        Assert.Single(mockWriter.WrittenObjects);

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal(2, written.id);
        Assert.Equal("/FlateDecode", written.dict["/Filter"]);
    }

    [Fact]
    public void WriteImage_WithUnsupportedFormat_ThrowsInvalidOperationException()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var invalidImage = new ImageInfo
        {
            Format = (ImageFormat)999, // Invalid format
            Width = 100,
            Height = 50
        };

        Assert.Throws<InvalidOperationException>(() => imageWriter.WriteImage(1, invalidImage, "/Im1"));
    }

    [Fact]
    public void WriteJpegImage_WritesCorrectDictionary()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var jpegImage = new ImageInfo
        {
            Format = ImageFormat.Jpeg,
            Width = 200,
            Height = 150,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }
        };

        imageWriter.WriteJpegImage(5, jpegImage, "/Im5");

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal("/XObject", written.dict["/Type"]);
        Assert.Equal("/Image", written.dict["/Subtype"]);
        Assert.Equal("200", written.dict["/Width"]);
        Assert.Equal("150", written.dict["/Height"]);
        Assert.Equal("/DeviceRGB", written.dict["/ColorSpace"]);
        Assert.Equal("8", written.dict["/BitsPerComponent"]);
        Assert.Equal("/DCTDecode", written.dict["/Filter"]);
    }

    [Fact]
    public void WriteJpegImage_WithGrayscale_UsesDeviceGray()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var grayscaleJpeg = new ImageInfo
        {
            Format = ImageFormat.Jpeg,
            Width = 100,
            Height = 100,
            BitsPerComponent = 8,
            Components = 1,
            ColorSpace = ImageColorSpace.DeviceGray,
            Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }
        };

        imageWriter.WriteJpegImage(1, grayscaleJpeg, "/Im1");

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal("/DeviceGray", written.dict["/ColorSpace"]);
    }

    [Fact]
    public void WriteJpegImage_WithCMYK_UsesDeviceCMYK()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var cmykJpeg = new ImageInfo
        {
            Format = ImageFormat.Jpeg,
            Width = 100,
            Height = 100,
            BitsPerComponent = 8,
            Components = 4,
            ColorSpace = ImageColorSpace.DeviceCMYK,
            Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }
        };

        imageWriter.WriteJpegImage(1, cmykJpeg, "/Im1");

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal("/DeviceCMYK", written.dict["/ColorSpace"]);
    }

    [Fact]
    public void WriteJpegImage_PreservesOriginalData()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        byte[] originalData = new byte[] { 0xFF, 0xD8, 0x00, 0x01, 0xFF, 0xD9 };
        var jpegImage = new ImageInfo
        {
            Format = ImageFormat.Jpeg,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = originalData
        };

        imageWriter.WriteJpegImage(1, jpegImage, "/Im1");

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal(originalData, written.data);
    }

    [Fact]
    public void WritePngImage_CompressesData()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[300], // 10x10 RGB = 300 bytes
            AlphaData = Array.Empty<byte>()
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        var written = mockWriter.WrittenObjects[0];
        // Compressed data should be smaller than original (for zeros)
        Assert.True(written.data.Length < pngImage.Data.Length);
    }

    [Fact]
    public void WritePngImage_WritesCorrectDictionary()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 50,
            Height = 75,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[50 * 75 * 3],
            AlphaData = Array.Empty<byte>()
        };

        imageWriter.WritePngImage(3, pngImage, "/Im3");

        var written = mockWriter.WrittenObjects[0];
        Assert.Equal("/XObject", written.dict["/Type"]);
        Assert.Equal("/Image", written.dict["/Subtype"]);
        Assert.Equal("50", written.dict["/Width"]);
        Assert.Equal("75", written.dict["/Height"]);
        Assert.Equal("/DeviceRGB", written.dict["/ColorSpace"]);
        Assert.Equal("8", written.dict["/BitsPerComponent"]);
        Assert.Equal("/FlateDecode", written.dict["/Filter"]);
    }

    [Fact]
    public void WritePngImage_WithAlpha_CreatesSoftMask()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            HasAlpha = true,
            Data = new byte[300], // 10x10 RGB
            AlphaData = new byte[100] // 10x10 alpha
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        // Should write 2 objects: main image + soft mask
        Assert.Equal(2, mockWriter.WrittenObjects.Count);

        // Main image should reference soft mask
        var mainImage = mockWriter.WrittenObjects.First(o => o.id == 1);
        Assert.True(mainImage.dict.ContainsKey("/SMask"));

        // Soft mask should be DeviceGray
        var softMask = mockWriter.WrittenObjects.First(o => o.id != 1);
        Assert.Equal("/DeviceGray", softMask.dict["/ColorSpace"]);
    }

    [Fact]
    public void WritePngImage_WithAlphaAndPdfA2b_CreatesSoftMask()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter)
        {
            Conformance = PdfAConformance.PdfA2b
        };

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            HasAlpha = true,
            Data = new byte[300],
            AlphaData = new byte[100]
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        Assert.Equal(2, mockWriter.WrittenObjects.Count);
        var mainImage = mockWriter.WrittenObjects.First(o => o.id == 1);
        Assert.True(mainImage.dict.ContainsKey("/SMask"));
    }

    [Fact]
    public void WritePngImage_WithAlphaAndPdfA1b_FlattensAlpha()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter)
        {
            Conformance = PdfAConformance.PdfA1b
        };

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            HasAlpha = true,
            Data = new byte[300],
            AlphaData = new byte[100]
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        Assert.Single(mockWriter.WrittenObjects);
        var mainImage = mockWriter.WrittenObjects[0];
        Assert.False(mainImage.dict.ContainsKey("/SMask"));
    }

    [Fact]
    public void WritePngImage_WithIndexedColor_UsesIndexedColorSpace()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        byte[] palette = new byte[256 * 3]; // 256 RGB colors
        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 1,
            ColorSpace = ImageColorSpace.Indexed,
            Data = new byte[100],
            Palette = palette,
            AlphaData = Array.Empty<byte>()
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        var written = mockWriter.WrittenObjects[0];
        Assert.Contains("[/Indexed /DeviceRGB", written.dict["/ColorSpace"]);
    }

    [Fact]
    public void WritePngImage_WithoutAlpha_DoesNotCreateSoftMask()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            HasAlpha = false,
            Data = new byte[300],
            AlphaData = Array.Empty<byte>()
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        // Should write only 1 object
        Assert.Single(mockWriter.WrittenObjects);

        // Should not have SMask
        var mainImage = mockWriter.WrittenObjects[0];
        Assert.False(mainImage.dict.ContainsKey("/SMask"));
    }

    [Fact]
    public void WritePngImage_CompressedDataHasZlibHeader()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var pngImage = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[300],
            AlphaData = Array.Empty<byte>()
        };

        imageWriter.WritePngImage(1, pngImage, "/Im1");

        var written = mockWriter.WrittenObjects[0];
        // Check for zlib header (0x78, 0x9C)
        Assert.Equal(0x78, written.data[0]);
        Assert.Equal(0x9C, written.data[1]);
    }

    [Fact]
    public void WritePngImage_MultipleImages_UsesUniqueIds()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        var image1 = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[300],
            AlphaData = Array.Empty<byte>()
        };

        var image2 = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 20,
            Height = 20,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[1200],
            AlphaData = Array.Empty<byte>()
        };

        imageWriter.WritePngImage(1, image1, "/Im1");
        imageWriter.WritePngImage(2, image2, "/Im2");

        Assert.Equal(2, mockWriter.WrittenObjects.Count);
        Assert.Equal(1, mockWriter.WrittenObjects[0].id);
        Assert.Equal(2, mockWriter.WrittenObjects[1].id);
    }

    private static ImageInfo CreateCmykJpeg() => new()
    {
        Format = ImageFormat.Jpeg,
        Width = 100,
        Height = 50,
        BitsPerComponent = 8,
        Components = 4,
        ColorSpace = ImageColorSpace.DeviceCMYK,
        Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }
    };

    private static ImageInfo CreateRgbJpeg() => new()
    {
        Format = ImageFormat.Jpeg,
        Width = 100,
        Height = 50,
        BitsPerComponent = 8,
        Components = 3,
        ColorSpace = ImageColorSpace.DeviceRGB,
        Data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }
    };

    [Fact]
    public void WriteJpegImage_NormalCmyk_OmitsDecodeArray()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        imageWriter.WriteJpegImage(1, CreateCmykJpeg(), "/Im1");

        Assert.False(mockWriter.WrittenObjects[0].dict.ContainsKey("/Decode"));
    }

    [Fact]
    public void WriteJpegImage_AdobeCmyk_EmitsInvertedDecodeArray()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);
        ImageInfo image = CreateCmykJpeg();
        image.InvertCmykColors = true;

        imageWriter.WriteJpegImage(1, image, "/Im1");

        Assert.Equal("[1 0 1 0 1 0 1 0]", mockWriter.WrittenObjects[0].dict["/Decode"]);
    }

    [Fact]
    public void WriteJpegImage_InvertFlagOnRgb_OmitsDecodeArray()
    {
        // A stray InvertCmykColors flag on a non-CMYK image must not produce
        // an 8-element /Decode array on a 3-channel image (which would be
        // malformed).
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);
        ImageInfo image = CreateRgbJpeg();
        image.InvertCmykColors = true;

        imageWriter.WriteJpegImage(1, image, "/Im1");

        Assert.False(mockWriter.WrittenObjects[0].dict.ContainsKey("/Decode"));
    }

    [Fact]
    public void WriteJpegImage_WithIccProfile_EmitsIccBasedColorSpace()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);
        ImageInfo image = CreateRgbJpeg();
        image.IccProfile = [0xAA, 0xBB, 0xCC, 0xDD];

        imageWriter.WriteJpegImage(1, image, "/Im1");

        // Two objects written: the ICC profile stream first, then the image.
        Assert.Equal(2, mockWriter.WrittenObjects.Count);

        var iccObj = mockWriter.WrittenObjects[0];
        Assert.Equal("3", iccObj.dict["/N"]); // RGB JPEG -> 3 components
        Assert.Equal("/DeviceRGB", iccObj.dict["/Alternate"]);
        Assert.Equal("/FlateDecode", iccObj.dict["/Filter"]);

        var imageObj = mockWriter.WrittenObjects[1];
        Assert.Equal($"[/ICCBased {iccObj.id} 0 R]", imageObj.dict["/ColorSpace"]);
    }

    [Fact]
    public void WriteJpegImage_CmykIccProfile_EmitsFourComponentIccStream()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);
        ImageInfo image = CreateCmykJpeg();
        image.IccProfile = [1, 2, 3];

        imageWriter.WriteJpegImage(1, image, "/Im1");

        var iccObj = mockWriter.WrittenObjects[0];
        Assert.Equal("4", iccObj.dict["/N"]);
        Assert.Equal("/DeviceCMYK", iccObj.dict["/Alternate"]);
    }

    [Fact]
    public void WriteJpegImage_WithoutIccProfile_PreservesDeviceColorSpace()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);

        imageWriter.WriteJpegImage(1, CreateRgbJpeg(), "/Im1");

        Assert.Single(mockWriter.WrittenObjects);
        Assert.Equal("/DeviceRGB", mockWriter.WrittenObjects[0].dict["/ColorSpace"]);
    }

    [Fact]
    public void WritePngImage_WithIccProfile_EmitsIccBasedColorSpace()
    {
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);
        var image = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 10,
            Height = 10,
            BitsPerComponent = 8,
            Components = 3,
            ColorSpace = ImageColorSpace.DeviceRGB,
            Data = new byte[300],
            AlphaData = Array.Empty<byte>(),
            IccProfile = [9, 8, 7]
        };

        imageWriter.WritePngImage(1, image, "/Im1");

        Assert.Equal(2, mockWriter.WrittenObjects.Count);
        var iccObj = mockWriter.WrittenObjects[0];
        Assert.Equal("3", iccObj.dict["/N"]);
        Assert.Equal("/DeviceRGB", iccObj.dict["/Alternate"]);
        Assert.Equal($"[/ICCBased {iccObj.id} 0 R]", mockWriter.WrittenObjects[1].dict["/ColorSpace"]);
    }

    [Fact]
    public void WritePngImage_IndexedWithIccProfile_KeepsIndexedPalette()
    {
        // ICC profiles never override an indexed palette - the palette bytes
        // are the colour definition for the image.
        var mockWriter = new MockPdfWriter();
        var imageWriter = new ImageWriter(mockWriter);
        var image = new ImageInfo
        {
            Format = ImageFormat.Png,
            Width = 2,
            Height = 2,
            BitsPerComponent = 8,
            Components = 1,
            ColorSpace = ImageColorSpace.Indexed,
            Data = new byte[4],
            AlphaData = Array.Empty<byte>(),
            Palette = [255, 0, 0, 0, 255, 0],
            IccProfile = [1, 2, 3]
        };

        imageWriter.WritePngImage(1, image, "/Im1");

        Assert.Single(mockWriter.WrittenObjects);
        Assert.StartsWith("[/Indexed /DeviceRGB 1 ", mockWriter.WrittenObjects[0].dict["/ColorSpace"]);
    }
}
