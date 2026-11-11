using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Hardening;

/// <summary>
/// Input that often comes from end users: image bytes, SVG markup, link targets, and trees built by code that can
/// go wrong. Each must fail inside the library with a catchable, descriptive error, or be bounded. Mirrors the
/// TypeScript hardening tests (spec 12 §3a, spec 11 §2, spec 01 §7, README "SVG import").
/// </summary>
[Collection("Serial")]
public partial class UntrustedInputTests
{
    // ---- PNG construction ----

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xedb88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        uint c = 0xffffffff;
        foreach (byte b in bytes) c = CrcTable[(c ^ b) & 0xff] ^ (c >> 8);
        return c ^ 0xffffffff;
    }

    private static void WriteChunk(MemoryStream output, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        output.Write(length);
        byte[] typeAndData = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type).CopyTo(typeAndData, 0);
        data.CopyTo(typeAndData, 4);
        output.Write(typeAndData);
        Span<byte> crc = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeAndData));
        output.Write(crc);
    }

    /// <summary>A PNG with the given header and a (possibly invalid) zlib stream. Colour type 0 is 8-bit grey, 6 is RGBA.</summary>
    private static byte[] Png(int width, int height, byte colorType, byte[] zlib, byte[]? iccp = null)
    {
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        byte[] ihdr = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)width);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)height);
        ihdr[8] = 8;
        ihdr[9] = colorType;
        WriteChunk(output, "IHDR", ihdr);
        if (iccp is not null) WriteChunk(output, "iCCP", iccp);
        WriteChunk(output, "IDAT", zlib);
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static byte[] Deflate(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var z = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) z.Write(raw);
        return output.ToArray();
    }

    private static readonly byte[] ValidPixels = Deflate(Filled((4 * 4 + 1) * 4, 128));

    private static byte[] Filled(int length, byte value)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    private static byte[] Generate(byte[] imageData)
    {
        using BonniePDF.Document doc = new();
        doc.Add(new Image { ImageData = imageData, AltText = "Test image", Width = 50, Height = 50 });
        doc.Add(new TextBlock("after the image"));
        return doc.GenerateBytes();
    }

    [Fact]
    public void ValidPngGeneratesCleanly() => Assert.NotEmpty(Generate(Png(4, 4, 6, ValidPixels)));

    [Fact]
    public void PixelDataTruncatedMidStreamIsAnImageDecodeException()
    {
        byte[] truncated = ValidPixels[..(ValidPixels.Length / 2)];
        var ex = Assert.Throws<ImageDecodeException>(() => Generate(Png(4, 4, 6, truncated)));
        Assert.Equal("PNG data is corrupt: its compressed stream could not be inflated.", ex.Message);
    }

    [Fact]
    public void PixelDataThatIsNotZlibIsAnImageDecodeException()
    {
        var ex = Assert.Throws<ImageDecodeException>(() => Generate(Png(4, 4, 6, Encoding.ASCII.GetBytes("this is not zlib data at all"))));
        Assert.Equal("PNG data is corrupt: its compressed stream could not be inflated.", ex.Message);
    }

    [Fact]
    public void HeaderClaimingSixtyThousandSquareIsRefusedBeforeAllocation()
    {
        var ex = Assert.Throws<ImageDecodeException>(() => Generate(Png(60000, 60000, 6, ValidPixels)));
        Assert.Equal("Image is 60000 x 60000 pixels, above the 100000000-pixel limit (ImageLoader.MaxPixels).", ex.Message);
    }

    [Fact]
    public void DecompressionBombIsRefusedRatherThanDecoded()
    {
        // 12,000 x 12,000 8-bit grey: well under 1 MB on the wire, 144 MB once inflated.
        const int side = 12000;
        byte[] bomb = Png(side, side, 0, Deflate(new byte[side * (side + 1)]));
        Assert.True(bomb.Length < 1024 * 1024);
        var watch = Stopwatch.StartNew();
        Assert.Throws<ImageDecodeException>(() => Generate(bomb));
        Assert.True(watch.ElapsedMilliseconds < 2000, $"refusing took {watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void DataThatInflatesPastItsHeaderIsAnImageDecodeException()
    {
        // A 4 x 4 header with pixel data for 4 x 400.
        var ex = Assert.Throws<ImageDecodeException>(() => Generate(Png(4, 4, 6, Deflate(new byte[(4 * 4 + 1) * 400]))));
        Assert.Equal("PNG data inflates to more than its header declares.", ex.Message);
    }

    [Fact]
    public void FourByFourRgbaMayInflateTo140BytesAndNoMore()
    {
        // Four rows of one filter byte and 16 samples, plus two bytes per row and 64 bytes of allowance.
        Assert.NotEmpty(Generate(Png(4, 4, 6, Deflate(Filled(140, 91)))));
        var ex = Assert.Throws<ImageDecodeException>(() => Generate(Png(4, 4, 6, Deflate(Filled(141, 91)))));
        Assert.Equal("PNG data inflates to more than its header declares.", ex.Message);
    }

    [Fact]
    public void AColourProfileThatCannotBeInflatedIsDropped()
    {
        byte[] iccp = [(byte)'p', 0, 0, .. Encoding.ASCII.GetBytes("not zlib")];
        string pdf = Encoding.Latin1.GetString(Generate(Png(4, 4, 6, Deflate(Filled(68, 92)), iccp)));
        Assert.DoesNotContain("/ICCBased", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void MaxPixelsCanBeChangedByTheApplication()
    {
        // Pixels no other test uses, so the process-wide image cache cannot already hold them.
        byte[] png = Png(4, 4, 6, Deflate(Filled((4 * 4 + 1) * 4, 77)));
        long original = ImageLoader.MaxPixels;
        try
        {
            ImageLoader.MaxPixels = 15;
            Assert.Throws<ImageDecodeException>(() => Generate(png));
            ImageLoader.MaxPixels = 16;
            Assert.NotEmpty(Generate(png));
        }
        finally
        {
            ImageLoader.MaxPixels = original;
        }
    }

    // ---- link URIs ----

    [GeneratedRegex(@"/URI \(((?:\\.|[^\\)])*)\)")]
    private static partial Regex UriEntry();

    private static string WrittenUri(string uri)
    {
        using BonniePDF.Document doc = new();
        doc.Add(new Hyperlink(uri, "link"));
        return UriEntry().Match(Encoding.Latin1.GetString(doc.GenerateBytes())).Groups[1].Value;
    }

    [Theory]
    [InlineData("https://example.com/a?b=c#d", "https://example.com/a?b=c#d")]
    [InlineData("https://example.com/café/日本", "https://example.com/caf%C3%A9/%E6%97%A5%E6%9C%AC")]
    [InlineData("https://example.com/caf%C3%A9", "https://example.com/caf%C3%A9")]
    [InlineData("https://example.com/a b", "https://example.com/a%20b")]
    public void LinkUrisAreWrittenAsSevenBitAscii(string uri, string written) => Assert.Equal(written, WrittenUri(uri));

    // ---- element cycles ----

    [Fact]
    public void AnElementThatContainsItselfIsReportedAsACycle()
    {
        var stack = new VerticalStack();
        stack.Add(new TextBlock("child"));
        stack.Add(stack);
        using BonniePDF.Document doc = new();
        doc.Add(stack);
        var ex = Assert.Throws<ElementCycleException>(() => doc.GenerateBytes());
        Assert.Equal("The element tree contains a cycle: a VerticalStack is its own descendant.", ex.Message);
    }

    [Fact]
    public void ReusingOneElementInTwoBranchesIsNotACycle()
    {
        var shared = new TextBlock("twice");
        using BonniePDF.Document doc = new();
        doc.Add(shared);
        doc.Add(shared);
        Assert.NotEmpty(doc.GenerateBytes());
    }

    // ---- SVG use expansion ----

    private static string UseBomb(int fanOut, int levels)
    {
        var defs = new StringBuilder("<rect id=\"r0\" width=\"1\" height=\"1\"/>");
        for (int level = 1; level <= levels; level++)
        {
            defs.Append($"<g id=\"r{level}\">");
            for (int i = 0; i < fanOut; i++) defs.Append($"<use href=\"#r{level - 1}\"/>");
            defs.Append("</g>");
        }
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><defs>{defs}</defs><use href=\"#r{levels}\"/></svg>";
    }

    [Fact]
    public void ModestSvgReuseIsDrawn()
    {
        using BonniePDF.Document doc = new();
        doc.Add(new Svg { SvgData = UseBomb(4, 2), Width = 50, Height = 50, AltText = "Test drawing" });
        Assert.NotEmpty(doc.GenerateBytes());
    }

    [Fact]
    public void SvgThatExpandsTo262144ShapesIsRefused()
    {
        string svg = UseBomb(8, 6);
        Assert.True(svg.Length < 2048);
        using BonniePDF.Document doc = new();
        doc.Add(new Svg { SvgData = svg, Width = 50, Height = 50, AltText = "Test drawing" });
        var ex = Assert.ThrowsAny<Exception>(() => doc.GenerateBytes());
        Assert.Contains("treated as runaway", ex.Message, StringComparison.Ordinal);
    }
}
