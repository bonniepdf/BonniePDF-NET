using System.Text;
using BonniePDF.Rendering.Writers;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>
/// Finished body pages are held as bytes while the rest of the document is laid out. These
/// tests pin the two things that must not change: the bytes written, and the order of page
/// chrome around a body that was sealed.
/// </summary>
public partial class FinishedPageStoreTests
{
    [System.Text.RegularExpressions.GeneratedRegex(@"/Contents (\d+) 0 R")]
    private static partial System.Text.RegularExpressions.Regex ContentsReference();

    [System.Text.RegularExpressions.GeneratedRegex(@"/Length (\d+)")]
    private static partial System.Text.RegularExpressions.Regex StreamLength();

    private static StringBuilder Page(string text) => new(text);

    [Fact]
    public void CompressedBodyIsTheStreamTheWriterWouldProduce()
    {
        const string text = "BT /F1 12 Tf 50 700 Td (Café £ 5) Tj ET\n";
        FinishedPageStore store = new() { Storage = FinishedPageStorage.Compressed };

        Assert.True(store.Seal(0, Page(text)));

        SealedPageBody body = Assert.IsType<SealedPageBody>(store.Take(0));
        Assert.True(body.Compressed);
        Assert.Equal(ContentCompression.Compress(Encoding.Latin1.GetBytes(text)), body.Bytes);
        Assert.Equal(text, Encoding.Latin1.GetString(body.Content()));
        Assert.Null(store.Take(0));
    }

    [Fact]
    public void TextOutsideLatin1StaysAsText()
    {
        // The writer would encode U+20AC as '?'; keeping the text leaves that decision to it.
        FinishedPageStore store = new() { Storage = FinishedPageStorage.Encoded };

        Assert.False(store.Seal(0, Page("(€) Tj\n")));
        Assert.Null(store.Take(0));
    }

    [Fact]
    public void KeepAndDiscardHoldNothing()
    {
        FinishedPageStore keep = new() { Storage = FinishedPageStorage.Keep };
        FinishedPageStore discard = new() { Storage = FinishedPageStorage.Discarded };

        Assert.False(keep.Seal(0, Page("q Q\n")));
        Assert.True(discard.Seal(0, Page("q Q\n")));
        Assert.Null(discard.Take(0));
    }

    [Fact]
    public void ChromeStillSurroundsASealedBody()
    {
        using BonniePDF.Document doc = new();
        for (int i = 0; i < 200; i++) doc.Add(new TextBlock { Text = $"BODY{i}", FontSize = 12 });
        doc.Settings.PageColor = Color.FromHex("#eeeeee");
        doc.Settings.PageBackground = _ => new TextBlock { Text = "BACKGROUND", FontSize = 12 };
        doc.Settings.HeaderTemplate = n => new TextBlock { Text = $"HEADER{n}", FontSize = 9 };
        doc.Settings.Watermark = new Watermark { Text = "WATERMARK" };

        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        List<PageRenderOutput> pages = PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).ToList();

        Assert.True(pages.Count > 2);
        // Every page but the last was sealed when layout left it.
        Assert.All(pages[..^1], page => Assert.NotNull(page.Body));
        for (int p = 0; p < pages.Count; p++)
        {
            string stream = pages[p].ContentStream;
            int fill = stream.IndexOf(" rg", StringComparison.Ordinal);
            int background = stream.IndexOf("(BACKGROUND)", StringComparison.Ordinal);
            int body = stream.IndexOf("(BODY", StringComparison.Ordinal);
            int header = stream.IndexOf($"(HEADER{p + 1})", StringComparison.Ordinal);
            int watermark = stream.IndexOf("WATERMARK", StringComparison.Ordinal);
            Assert.True(fill >= 0 && fill < background && background < body && body < header && header < watermark,
                $"page {p}: fill {fill}, background {background}, body {body}, header {header}, watermark {watermark}");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WrittenStreamsDecodeToTheRenderedPages(bool chrome)
    {
        BonniePDF.Document Build()
        {
            BonniePDF.Document doc = new();
            for (int i = 0; i < 300; i++) doc.Add(new TextBlock { Text = $"Row {i} \u00fc\u00e9\u00df", FontSize = 12 });
            if (chrome) doc.Settings.FooterTemplate = n => new TextBlock { Text = $"Page {n}", FontSize = 9 };
            return doc;
        }

        List<string> expected;
        using (BonniePDF.Document doc = Build())
        {
            RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
            expected = PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).Select(p => p.ContentStream).ToList();
        }
        byte[] file;
        using (BonniePDF.Document doc = Build()) file = doc.GenerateBytes();

        List<string> written = ContentStreams(file);
        Assert.True(expected.Count > 2);
        Assert.Equal(expected, written);
    }

    /// <summary>Each page's decoded content stream, in page order.</summary>
    private static List<string> ContentStreams(byte[] file)
    {
        string text = Encoding.Latin1.GetString(file);
        var streams = new List<string>();
        foreach (System.Text.RegularExpressions.Match contents in ContentsReference().Matches(text))
        {
            int start = text.IndexOf($"\n{contents.Groups[1].Value} 0 obj", StringComparison.Ordinal);
            var length = StreamLength().Match(text, start);
            int data = text.IndexOf("stream\n", start, StringComparison.Ordinal) + "stream\n".Length;
            byte[] compressed = file.AsSpan(data, int.Parse(length.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            streams.Add(Encoding.Latin1.GetString(ContentCompression.Decompress(compressed)));
        }
        return streams;
    }
}
