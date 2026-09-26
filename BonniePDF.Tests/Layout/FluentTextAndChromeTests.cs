using System.IO.Compression;
using System.Text.RegularExpressions;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Two fluent-API behaviours that were silently losing content: styling applied to a rich-text
/// span after it was added, and a footer whose content sits in a container.
/// </summary>
public partial class FluentTextAndChromeTests
{
    // A font selection followed by the text it draws: (font resource, text).
    [GeneratedRegex(@"/F(\d+) [\d.]+ Tf\s*\((.*?)\) Tj")]
    private static partial Regex FontThenText();

    private static List<PageRenderOutput> RenderPages(BonniePDF.Document doc)
    {
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).ToList();
    }

    [Fact]
    public void ASpanStyledAfterItIsAdded_KeepsItsStyle()
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
            page.Content().Text(t =>
            {
                t.Span("Label: ").Bold();
                t.Span("value");
                t.Span("Red").FontColor(BonniePDF.Core.Color.Red);
            }))).Build();

        string stream = RenderPages(doc).Single().ContentStream;
        Dictionary<string, string> fontOf = FontThenText().Matches(stream).ToDictionary(m => m.Groups[2].Value, m => m.Groups[1].Value);

        // Three spans, three runs - not one merged run in the default style.
        Assert.Contains("Label: ", fontOf.Keys);
        Assert.Contains("value", fontOf.Keys);
        Assert.NotEqual(fontOf["Label: "], fontOf["value"]); // bold is a different font from regular
        Assert.Contains("1 0 0 rg", stream); // the red span's colour
    }

    [Fact]
    public void ALineSpanStyledAfterItIsAdded_KeepsItsStyle()
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
            page.Content().Text(t =>
            {
                t.Line("Heading").Bold();
                t.Span("body");
            }))).Build();

        Dictionary<string, string> fontOf = FontThenText().Matches(RenderPages(doc).Single().ContentStream)
            .ToDictionary(m => m.Groups[2].Value, m => m.Groups[1].Value);

        Assert.NotEqual(fontOf["Heading"], fontOf["body"]);
    }

    [Fact]
    public void RichText_InheritsThePageDefaultTextStyle()
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.DefaultTextStyle(x => x.WithSize(20));
            page.Content().Text(t =>
            {
                t.Span("inherits");
                t.Span("overrides").FontSize(9);
            });
        })).Build();

        string stream = RenderPages(doc).Single().ContentStream;

        Assert.Matches(SizeThenText("inherits"), stream);
        Assert.Matches(SizeThenText("overrides", 9), stream);
    }

    // A font selection at the given size followed by the text it draws.
    private static string SizeThenText(string text, int size = 20) => @"/F\d+ " + size + @" Tf\s*\(" + text + @"\) Tj";

    [Fact]
    public void AFooterInAContainer_IsDrawn_WithoutBreakingThePage()
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Content().Text("body");
            page.Footer().AlignCenter().Text("FOOTERMARK");
        })).Build();

        List<PageRenderOutput> pages = RenderPages(doc);

        Assert.Single(pages); // the footer did not push a second page out
        Assert.Contains("FOOTERMARK", pages[0].ContentStream);
    }

    [Fact]
    public void ARichTextFooterInAContainer_IsDrawn()
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Content().Text("body");
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Page ");
                t.CurrentPageNumber();
            });
        })).Build();

        Assert.Contains("Page ", RenderPages(doc).Single().ContentStream);
    }

    // ---- Page tokens in rich text ----

    [GeneratedRegex(@"(?<!end)stream\r?\n")]
    private static partial Regex StreamKeyword();

    // Every FlateDecode stream in a generated PDF, inflated, as text.
    private static List<string> InflatedStreams(byte[] pdf)
    {
        string text = System.Text.Encoding.Latin1.GetString(pdf);
        var streams = new List<string>();
        foreach (Match m in StreamKeyword().Matches(text))
        {
            // The stream's dictionary ends at the keyword; its /Length is the last one before it.
            string before = text.Substring(Math.Max(0, m.Index - 600), Math.Min(600, m.Index));
            MatchCollection lengths = LengthEntry().Matches(before);
            if (lengths.Count == 0) continue;
            Match length = lengths[^1];
            try
            {
                using var inflated = new MemoryStream();
                using (var z = new ZLibStream(new MemoryStream(pdf, m.Index + m.Length, int.Parse(length.Groups[1].Value)), CompressionMode.Decompress))
                    z.CopyTo(inflated);
                streams.Add(System.Text.Encoding.Latin1.GetString(inflated.ToArray()));
            }
            catch (InvalidDataException)
            {
                // Not a Flate stream (an image or font program); nothing to read.
            }
        }

        return streams;
    }

    [GeneratedRegex(@"/Length (\d+)")]
    private static partial Regex LengthEntry();

    private static BonniePDF.Document TwoPageDocumentWithFooter(Action<BonniePDF.Fluent.Descriptors.TextDescriptor> footer) =>
        Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Margin(50);
            page.Content().Column(c => { for (int i = 0; i < 90; i++) c.Item().Text("line " + i); });
            page.Footer().AlignCenter().Text(footer);
        })).Build();

    [Fact]
    public void FluentPageNumbers_AreSubstitutedOnEveryPage()
    {
        using BonniePDF.Document doc = TwoPageDocumentWithFooter(t =>
        {
            t.Span("Page ");
            t.CurrentPageNumber();
            t.Span(" of ");
            t.TotalPages();
        });

        List<PageRenderOutput> pages = RenderPages(doc);

        Assert.Equal(2, pages.Count);
        for (int p = 0; p < pages.Count; p++)
        {
            string stream = pages[p].ContentStream;
            Assert.DoesNotContain("{{", stream);
            Assert.Contains("(Page " + (p + 1) + " of ) Tj", stream); // this page's own number, in one run
            Assert.Contains(" Do", stream);                 // the deferred total
        }
    }

    [Fact]
    public void TheTotalPageCount_ResolvesInTheFinishedPdf()
    {
        using BonniePDF.Document doc = TwoPageDocumentWithFooter(t =>
        {
            t.Span("of ");
            t.TotalPages();
        });

        byte[] pdf = doc.GenerateBytes();

        // The deferred total is a small uncompressed form object; everything else is Flate, so look at both.
        var everything = new List<string>(InflatedStreams(pdf)) { System.Text.Encoding.Latin1.GetString(pdf) };

        Assert.DoesNotContain(everything, s => s.Contains("{{TotalPages}}"));
        Assert.Contains(everything, s => s.Contains("(2) Tj")); // the deferred object holds the real total
    }

    [Fact]
    public void ARichTextElementSubstitutesPageTokensDirectly()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.FooterTemplate = _ => new RichText { Alignment = TextAlignment.Center }
            .Span("Page {{CurrentPage}}");
        doc.Add(new TextBlock { Text = "body" });

        string stream = RenderPages(doc).Single().ContentStream;

        Assert.DoesNotContain("{{", stream);
        Assert.Contains("(Page 1) Tj", stream);
    }

    [Fact]
    public void RichTextWithoutTokens_IsUnchanged()
    {
        using BonniePDF.Document doc = new();
        doc.Add(new RichText().Span("no tokens here").Span(" or here"));

        string stream = RenderPages(doc).Single().ContentStream;

        Assert.Contains("(no tokens here or here) Tj", stream); // adjacent same-style spans are one run
        Assert.DoesNotContain(" Do", stream);
    }
}
