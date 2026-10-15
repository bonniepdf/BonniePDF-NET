using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>
/// The writers count the bytes they write instead of asking the destination for its position,
/// so a PDF can be saved to a stream that cannot seek (an HTTP response body, a pipe), and the
/// destination is flushed once at the end rather than before every object.
/// </summary>
public partial class PdfOutputStreamTests
{
    [GeneratedRegex(@"startxref\s+(\d+)")]
    private static partial Regex StartXref();

    [GeneratedRegex(@"(\d{10}) 00000 n")]
    private static partial Regex XrefEntry();

    private static BonniePDF.Document Sample()
    {
        var doc = new BonniePDF.Document();
        for (int i = 0; i < 300; i++) doc.Add(new TextBlock($"Line {i} of a document long enough to span several pages."));
        return doc;
    }

    /// <summary>Every in-use cross-reference entry, relative to <paramref name="origin"/>, points at its object.</summary>
    private static void AssertXrefResolves(byte[] file, int origin = 0)
    {
        string text = Encoding.Latin1.GetString(file);
        int xref = int.Parse(StartXref().Matches(text)[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - origin;
        Assert.StartsWith("xref", text[xref..], StringComparison.Ordinal);
        int id = 0;
        foreach (Match entry in XrefEntry().Matches(text[xref..]))
        {
            id++;
            int offset = int.Parse(entry.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - origin;
            Assert.Matches(@"^\d+ 0 obj", text[offset..(offset + 16)]);
        }
        Assert.True(id > 10);
    }

    [Fact]
    public void SavesToAStreamThatCannotSeek()
    {
        using var doc = Sample();
        var destination = new ForwardOnlyStream();

        doc.Save(destination, leaveOpen: true);

        AssertXrefResolves(destination.Written.ToArray());
        Assert.Equal(1, destination.Flushes);
    }

    [Fact]
    public async Task SavesToAStreamThatCannotSeekAsynchronously()
    {
        using var doc = Sample();
        var destination = new ForwardOnlyStream();

        await doc.SaveAsync(destination, leaveOpen: true);

        AssertXrefResolves(destination.Written.ToArray());
        Assert.Equal(1, destination.Flushes);
    }

    [Fact]
    public void OffsetsStillCountFromTheDestinationsPosition()
    {
        using var doc = Sample();
        using var destination = new MemoryStream();
        byte[] prefix = Encoding.ASCII.GetBytes("already here\n");
        destination.Write(prefix);

        doc.Save(destination, leaveOpen: true);

        // As before: offsets are the destination's own positions, prefix included.
        AssertXrefResolves(destination.ToArray()[prefix.Length..], origin: prefix.Length);
    }

    /// <summary>A write-only stream with no position, like an HTTP response body.</summary>
    private sealed class ForwardOnlyStream : Stream
    {
        public MemoryStream Written { get; } = new();
        public int Flushes { get; private set; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => Flushes++;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => Written.Write(buffer, offset, count);
    }
}
