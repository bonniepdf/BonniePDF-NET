using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.IO;
using BonniePDF.Models;
using Xunit;

namespace BonniePDF.Tests;

public class ImageWriterTests
{
    // Mock implementation of IPdfWriter for testing
    private class MockPdfWriter : IPdfWriter
    {
        public List<(int id, Dictionary<string, string> dict, byte[] data)> WrittenObjects { get; } = new();
        private int _nextId = 2; // Start from 2 to avoid conflicts with test-provided IDs

        public BonniePDF.Security.Encryption.PdfEncryptor Encryptor { get; set; }

        public int GetNewId() => _nextId++;

        public void WriteBinaryObject(int id, Dictionary<string, string> dict, byte[] data)
        {
            WrittenObjects.Add((id, dict, data));
        }

        public void WriteObject(int id, Dictionary<string, string> dict, string streamContent = null)
        {
            throw new NotImplementedException();
        }

        public void WriteCompressedObject(int id, Dictionary<string, string> dict, string content)
        {
            throw new NotImplementedException();
        }

        public void WriteCompressedBinaryObject(int id, Dictionary<string, string> dict, byte[] streamContent)
        {
            throw new NotImplementedException();
        }

        public int WriteLinkAnnotation(int id, BonniePDF.Models.Hyperlink hyperlink)
        {
            throw new NotImplementedException();
        }

        public int WriteDocumentInfo(int id, BonniePDF.Metadata.DocumentMetadata metadata)
        {
            throw new NotImplementedException();
        }

        public int WriteViewerPreferences(int id, BonniePDF.Configuration.DocumentSettings settings)
        {
            throw new NotImplementedException();
        }

        public void WriteCatalog(int id, int pagesId, int firstPageId, BonniePDF.Configuration.DocumentSettings settings, BonniePDF.IO.CatalogOptions options)
        {
            throw new NotImplementedException();
        }

        public void WriteUnencryptedBinaryObject(int id, Dictionary<string, string> dict, byte[] streamContent)
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
}
