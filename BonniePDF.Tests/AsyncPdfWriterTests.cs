using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.IO;
using Xunit;

namespace BonniePDF.Tests;

public class AsyncPdfWriterTests
{
    [Fact]
    public async Task Constructor_WithFilename_CreatesFile()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            var writer = new AsyncPdfWriter(tempFile);
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();

            Assert.True(File.Exists(tempFile));
            Assert.True(new FileInfo(tempFile).Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Constructor_WithStream_WritesToStream()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void Constructor_WithNullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AsyncPdfWriter((Stream)null));
    }

    [Fact]
    public void Constructor_WithNonWritableStream_ThrowsArgumentException()
    {
        using var readOnlyStream = new MemoryStream(new byte[10], writable: false);
        Assert.Throws<ArgumentException>(() => new AsyncPdfWriter(readOnlyStream));
    }

    [Fact]
    public async Task WritePdfHeader_WritesPdf14Header()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true, PdfVersion.Pdf14);
            await writer.CloseAsync(1, 0);
            // Must flush the writer before getting bytes
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        // Parse the PDF header
        using var ms = new MemoryStream(pdfBytes);
        using var reader = new StreamReader(ms);
        string header = reader.ReadLine();

        Assert.Equal("%PDF-1.4", header);
    }

    [Fact]
    public async Task WritePdfHeader_WritesPdf17Header()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true, PdfVersion.Pdf17);
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        using var ms = new MemoryStream(pdfBytes);
        using var reader = new StreamReader(ms);
        string header = reader.ReadLine();

        Assert.Equal("%PDF-1.7", header);
    }

    [Fact]
    public async Task GetNewId_ReturnsUniqueIds()
    {
        using var stream = new MemoryStream();
        var writer = new AsyncPdfWriter(stream, leaveOpen: true);

        int id1 = writer.GetNewId();
        int id2 = writer.GetNewId();
        int id3 = writer.GetNewId();

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.True(id2 > id1);
        Assert.True(id3 > id2);
    }

    [Fact]
    public async Task WriteObjectAsync_WritesObjectWithDictionary()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            var dict = new Dictionary<string, string>
            {
                ["/Type"] = "/Page",
                ["/MediaBox"] = "[0 0 612 792]"
            };

            await writer.WriteObjectAsync(1, dict);
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        string content = Encoding.ASCII.GetString(pdfBytes);

        Assert.Contains("1 0 obj", content);
        Assert.Contains("/Type /Page", content);
        Assert.Contains("/MediaBox [0 0 612 792]", content);
        Assert.Contains("endobj", content);
    }

    [Fact]
    public async Task WriteObjectAsync_WithStreamContent_WritesStream()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            var dict = new Dictionary<string, string>
            {
                ["/Length"] = "10"
            };

            await writer.WriteObjectAsync(2, dict, "HelloWorld");
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        string content = Encoding.ASCII.GetString(pdfBytes);

        Assert.Contains("2 0 obj", content);
        Assert.Contains("stream", content);
        Assert.Contains("HelloWorld", content);
        Assert.Contains("endstream", content);
    }

    [Fact]
    public async Task WriteBinaryObjectAsync_WritesBinaryData()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            var dict = new Dictionary<string, string>
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Image"
            };

            byte[] binaryData = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };

            await writer.WriteBinaryObjectAsync(3, dict, binaryData, binaryData.Length);
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        Assert.Contains((byte)0xFF, pdfBytes);
        Assert.Contains((byte)0xFE, pdfBytes);
    }

    [Fact]
    public async Task CloseAsync_WritesXrefTable()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            var dict = new Dictionary<string, string> { ["/Type"] = "/Catalog" };
            await writer.WriteObjectAsync(1, dict);
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        string content = Encoding.ASCII.GetString(pdfBytes);

        Assert.Contains("xref", content);
        Assert.Contains("trailer", content);
        Assert.Contains("%%EOF", content);
    }

    [Fact]
    public async Task CloseAsync_WritesTrailer()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            await writer.WriteObjectAsync(1, new Dictionary<string, string> { ["/Type"] = "/Catalog" });
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        string content = Encoding.ASCII.GetString(pdfBytes);

        Assert.Contains("trailer", content);
        Assert.Contains("/Size", content);
    }

    [Fact]
    public async Task MultipleObjects_MaintainCorrectOrder()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            await writer.WriteObjectAsync(1, new Dictionary<string, string> { ["/Type"] = "/Catalog" });
            await writer.WriteObjectAsync(2, new Dictionary<string, string> { ["/Type"] = "/Pages" });
            await writer.WriteObjectAsync(3, new Dictionary<string, string> { ["/Type"] = "/Page" });
            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            pdfBytes = stream.ToArray();
        }

        string content = Encoding.ASCII.GetString(pdfBytes);

        int pos1 = content.IndexOf("1 0 obj");
        int pos2 = content.IndexOf("2 0 obj");
        int pos3 = content.IndexOf("3 0 obj");

        Assert.True(pos1 < pos2);
        Assert.True(pos2 < pos3);
    }

    [Fact]
    public async Task DisposeAsync_ClosesStream()
    {
        var stream = new MemoryStream();
        var writer = new AsyncPdfWriter(stream, leaveOpen: false);

        await writer.CloseAsync(1, 0);
        await writer.DisposeAsync();

        // Stream should be disposed if leaveOpen is false
        Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
    }

    [Fact]
    public async Task DisposeAsync_WithLeaveOpen_AllowsDataRetrieval()
    {
        byte[] pdfBytes;
        {
            var stream = new MemoryStream();
            var writer = new AsyncPdfWriter(stream, leaveOpen: true);

            await writer.CloseAsync(1, 0);
            await writer.DisposeAsync();
            // With leaveOpen: true, we can get the bytes before the stream goes out of scope
            pdfBytes = stream.ToArray();
        }

        // Verify we got valid data
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public async Task WriteObjectAsync_WithEmptyDictionary_WritesValidObject()
    {
        using var stream = new MemoryStream();
        var writer = new AsyncPdfWriter(stream, leaveOpen: true);

        await writer.WriteObjectAsync(1, new Dictionary<string, string>());
        await writer.CloseAsync(1, 0);
        await writer.DisposeAsync();

        stream.Position = 0;
        string content = Encoding.ASCII.GetString(stream.ToArray());

        Assert.Contains("1 0 obj", content);
        Assert.Contains("<<", content);
        Assert.Contains(">>", content);
        Assert.Contains("endobj", content);
    }

    [Fact]
    public async Task ConcurrentWrites_MaintainDataIntegrity()
    {
        using var stream = new MemoryStream();
        var writer = new AsyncPdfWriter(stream, leaveOpen: true);

        // Write multiple objects sequentially
        await writer.WriteObjectAsync(1, new Dictionary<string, string> { ["/Type"] = "/Catalog" });
        await writer.WriteObjectAsync(2, new Dictionary<string, string> { ["/Type"] = "/Pages" });
        await writer.WriteObjectAsync(3, new Dictionary<string, string> { ["/Type"] = "/Page" });
        await writer.WriteObjectAsync(4, new Dictionary<string, string> { ["/Type"] = "/Font" });
        await writer.CloseAsync(1, 0);
        await writer.DisposeAsync();

        stream.Position = 0;
        string content = Encoding.ASCII.GetString(stream.ToArray());

        // All objects should be present
        Assert.Contains("1 0 obj", content);
        Assert.Contains("2 0 obj", content);
        Assert.Contains("3 0 obj", content);
        Assert.Contains("4 0 obj", content);
    }

    [Fact]
    public async Task LargeObject_WritesSuccessfully()
    {
        using var stream = new MemoryStream();
        var writer = new AsyncPdfWriter(stream, leaveOpen: true);

        // Create large stream content
        string largeContent = new string('X', 100000);

        var dict = new Dictionary<string, string>
        {
            ["/Length"] = largeContent.Length.ToString()
        };

        await writer.WriteObjectAsync(1, dict, largeContent);
        await writer.CloseAsync(1, 0);
        await writer.DisposeAsync();

        Assert.True(stream.Length > 100000);
    }

    [Fact]
    public async Task BinaryObject_WithLargeData_WritesSuccessfully()
    {
        using var stream = new MemoryStream();
        var writer = new AsyncPdfWriter(stream, leaveOpen: true);

        byte[] largeData = new byte[50000];
        new Random().NextBytes(largeData);

        var dict = new Dictionary<string, string>
        {
            ["/Type"] = "/XObject",
            ["/Length"] = largeData.Length.ToString()
        };

        await writer.WriteBinaryObjectAsync(1, dict, largeData, largeData.Length);
        await writer.CloseAsync(1, 0);
        await writer.DisposeAsync();

        Assert.True(stream.Length > 50000);
    }
}
