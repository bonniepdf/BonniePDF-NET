using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>
/// The synchronous and asynchronous writers must produce the same PDF for the same document,
/// including when stream compression is switched off.
/// </summary>
[Collection("Serial")]
public partial class SyncAsyncCompressionTests
{
    private static BonniePDF.Document Build()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA2b;
        doc.Add(new TextBlock { Text = "Embedded font, uncompressed streams.", FontSize = 14 });
        doc.Add(new TextBlock { Text = "A second paragraph so the subset holds several glyphs.", FontSize = 12 });
        return doc;
    }

    [GeneratedRegex(@"/ID \[<[0-9A-Fa-f]+> ?<[0-9A-Fa-f]+>\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FileId();

    [GeneratedRegex(@"/(?<key>CreationDate|ModDate) \([^)]*\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InfoDates();

    [GeneratedRegex(@"(?<open><xmp:(?:CreateDate|ModifyDate|MetadataDate)[^>]*>)[^<]*", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex XmpDates();

    [GeneratedRegex(@"(?<open><xmpMM:(?:DocumentID|InstanceID)[^>]*>)[^<]*", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex XmpIds();

    [GeneratedRegex(@"\b[A-Z]{6}\+(?=[A-Za-z])", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SubsetPrefix();

    /// <summary>Masks what legitimately differs between two generations: the file ID, the dates and the random subset prefixes.</summary>
    private static string Normalise(byte[] bytes)
    {
        string s = Encoding.Latin1.GetString(bytes);
        s = FileId().Replace(s, "/ID []");
        s = InfoDates().Replace(s, "/${key} ()");
        s = XmpDates().Replace(s, "${open}");
        s = XmpIds().Replace(s, "${open}");
        return SubsetPrefix().Replace(s, "SUBSET+");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SyncAndAsyncOutput_AreIdentical_WhetherOrNotStreamsAreCompressed(bool compression)
    {
        InternalConfiguration.EnableCompression = compression;
        try
        {
            using BonniePDF.Document sync = Build();
            using BonniePDF.Document async = Build();

            string expected = Normalise(sync.GenerateBytes());
            string actual = Normalise(await async.GenerateBytesAsync());

            Assert.Equal(expected, actual);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }
}
