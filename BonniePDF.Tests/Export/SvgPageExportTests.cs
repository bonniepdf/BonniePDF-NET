using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Tests.Support;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Export;

public class SvgPageExportTests
{
    private static byte[] CreateMinimalJpeg() =>
    [
        0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03,
        0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00, 0xFF, 0xD9
    ];

    private static BonniePDF.Document SimpleDoc()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Hello, SVG world!", FontSize = 14 });
        doc.Add(new TextBlock { Text = "Second line of text.", FontSize = 11 });
        return doc;
    }

    [Fact]
    public void GenerateSvgPages_ReturnsOneSvgPerPage()
    {
        IReadOnlyList<string> pages = SimpleDoc().GenerateSvgPages();
        Assert.Single(pages);
        Assert.Contains("<svg", pages[0], StringComparison.Ordinal);
        Assert.Contains("</svg>", pages[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FirstPageSvg_HasViewBoxAndDimensions()
    {
        string svg = SimpleDoc().GenerateFirstPageSvg();
        Assert.Contains("viewBox=\"0 0 ", svg, StringComparison.Ordinal);
        Assert.Contains("width=\"", svg, StringComparison.Ordinal);
        Assert.Contains("height=\"", svg, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SvgShapeRendering.OptimizeSpeed, "optimizeSpeed")]
    [InlineData(SvgShapeRendering.CrispEdges, "crispEdges")]
    [InlineData(SvgShapeRendering.GeometricPrecision, "geometricPrecision")]
    public void ShapeRendering_IsWrittenOnTheRootAfterTheViewBox(SvgShapeRendering mode, string token)
    {
        string svg = SimpleDoc().GenerateFirstPageSvg(new SvgPageExportOptions { ShapeRendering = mode });
        Assert.Matches($"^<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"[^\"]+\" height=\"[^\"]+\" viewBox=\"[^\"]+\" shape-rendering=\"{token}\"", svg);
    }

    [Fact]
    public void ShapeRendering_DefaultWritesNoAttribute()
    {
        string svg = SimpleDoc().GenerateFirstPageSvg();
        Assert.DoesNotContain("shape-rendering", svg.Split('>')[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FirstPageSvg_ContainsTextContent()
    {
        string svg = SimpleDoc().GenerateFirstPageSvg();
        Assert.Contains("Hello, SVG world!", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstPageSvg_WrapsContentInPageGroup()
    {
        string svg = SimpleDoc().GenerateFirstPageSvg();
        Assert.Contains("<g id=\"page-1\" data-bonniepdf-page=\"1\">", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessibilityMetadata_OnByDefault_OffByOption()
    {
        string with = SimpleDoc().GenerateFirstPageSvg();
        Assert.Contains("role=\"img\"", with, StringComparison.Ordinal);
        Assert.Contains("<title>", with, StringComparison.Ordinal);

        string without = SimpleDoc().GenerateFirstPageSvg(new SvgPageExportOptions { IncludeAccessibilityMetadata = false });
        Assert.DoesNotContain("role=\"img\"", without, StringComparison.Ordinal);
        Assert.DoesNotContain("<title>", without, StringComparison.Ordinal);
    }

    [Fact]
    public void Output_IsDeterministicAcrossEquivalentDocuments()
    {
        string a = SimpleDoc().GenerateFirstPageSvg();
        string b = SimpleDoc().GenerateFirstPageSvg();
        Assert.Equal(a, b);
    }

    [Fact]
    public void MultiPageDocument_ExportsEveryPage()
    {
        var doc = new BonniePDF.Document();
        for (int i = 0; i < 3; i++)
        {
            if (i > 0) doc.AddPage();
            doc.Add(new TextBlock { Text = $"Page {i + 1}" });
        }
        IReadOnlyList<string> pages = doc.GenerateSvgPages();
        Assert.True(pages.Count >= 3);
        for (int i = 0; i < pages.Count; i++)
            Assert.Contains($"data-bonniepdf-page=\"{i + 1}\"", pages[i], StringComparison.Ordinal);
    }

    [Fact]
    public void MaxPages_LimitsOutput()
    {
        var doc = new BonniePDF.Document();
        for (int i = 0; i < 4; i++)
        {
            if (i > 0) doc.AddPage();
            doc.Add(new TextBlock { Text = $"Page {i + 1}" });
        }
        IReadOnlyList<string> pages = doc.GenerateSvgPages(new SvgPageExportOptions { MaxPages = 2 });
        Assert.Equal(2, pages.Count);
        Assert.Contains("data-bonniepdf-page=\"1\"", pages[0], StringComparison.Ordinal);
        Assert.Contains("data-bonniepdf-page=\"2\"", pages[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Images_EmbeddedAsDataUri()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new Image { ImageData = CreateMinimalJpeg(), Width = 64, AltText = "sample" });
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("<image", svg, StringComparison.Ordinal);
        Assert.Contains("data:image/jpeg;base64,", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Images_OmittedWhenRequested()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new Image { ImageData = CreateMinimalJpeg(), Width = 64, AltText = "sample" });
        string svg = doc.GenerateFirstPageSvg(new SvgPageExportOptions { EmbedImages = SvgImageEmbedding.Omit });
        Assert.DoesNotContain("<image", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Charts_RenderIntoPage()
    {
        var doc = new BonniePDF.Document();
        var chart = new BarChart { Width = 400, Height = 250 };
        var data = new ChartData
        {
            Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }, new() { Label = "B", Value = 20 }] }]
        };
        chart.Data = data;
        doc.Add(chart);
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("<rect", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Tables_Render()
    {
        var doc = new BonniePDF.Document();
        var table = new Table(80, 80);
        table.AddRow(new TextBlock { Text = "Cell A" }, new TextBlock { Text = "Cell B" });
        doc.Add(table);
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("Cell A", svg, StringComparison.Ordinal);
        Assert.Contains("Cell B", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Watermark_RendersOverContent()
    {
        var doc = SimpleDoc();
        doc.Settings.Watermark = new Watermark { Text = "DRAFT" };
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("DRAFT", svg, StringComparison.Ordinal);
        Assert.Contains("rotate(45", svg, StringComparison.Ordinal); // default -45deg negated for SVG
        // Watermark is appended (paints on top of the body content so it is never occluded).
        int wmIndex = svg.IndexOf("DRAFT", StringComparison.Ordinal);
        int bodyIndex = svg.IndexOf("Hello, SVG world!", StringComparison.Ordinal);
        Assert.True(wmIndex > bodyIndex);
    }

    [Fact]
    public void HeaderAndFooter_RenderOnPage()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Body" });
        doc.Settings.HeaderTemplate = pageNum => new TextBlock { Text = $"Header {pageNum}", FontSize = 9, Color = ColorPalette.Slate[500] };
        doc.Settings.FooterTemplate = pageNum => new TextBlock { Text = $"Footer {pageNum}", FontSize = 9, Color = ColorPalette.Slate[500] };
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("Header 1", svg, StringComparison.Ordinal);
        Assert.Contains("Footer 1", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TotalPagesToken_ResolvesInSvgOutput_WhenMaxPagesLimitsSerialization()
    {
        var doc = new BonniePDF.Document();
        for (int i = 0; i < 3; i++)
        {
            if (i > 0) doc.AddPage();
            doc.Add(new TextBlock { Text = $"Body {i + 1}" });
        }
        doc.Settings.FooterTemplate = pageNum => new TextBlock
        {
            Text = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
            FontSize = 9
        };

        IReadOnlyList<string> pages = doc.GenerateSvgPages(new SvgPageExportOptions { MaxPages = 2 });

        Assert.Equal(2, pages.Count);
        Assert.Contains("Page 1 of ", pages[0], StringComparison.Ordinal);
        Assert.Contains("Page 2 of ", pages[1], StringComparison.Ordinal);
        foreach (string page in pages)
        {
            Assert.Contains("data-bonniepdf-total-pages=\"true\">3</text>", page, StringComparison.Ordinal);
            Assert.DoesNotContain("??", page, StringComparison.Ordinal);
            Assert.DoesNotContain(TextElementBase.TotalPagesToken, page, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OutOfRangePageIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimpleDoc().GenerateSvgPage(5));
    }

    [Fact]
    public void SpecialCharacters_AreEscaped()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "a < b & c > d" });
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("&lt;", svg, StringComparison.Ordinal);
        Assert.Contains("&amp;", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("a < b", svg, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task AsyncApi_MatchesSync()
    {
        IReadOnlyList<string> sync = SimpleDoc().GenerateSvgPages();
        IReadOnlyList<string> async = await SimpleDoc().GenerateSvgPagesAsync();
        Assert.Equal(sync.Count, async.Count);
        Assert.Equal(sync[0], async[0]);
    }

    [Fact]
    public void StandardFontBoldItalic_MapToCssWeightAndStyle()
    {
        // The raw PDF name ("Helvetica-Bold") is not a CSS family; emitting it
        // would render a regular default. Standard-14 fonts are viewer-supplied,
        // so prefer the real OS font via a generic stack plus weight/style.
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Bold", FontName = StandardFonts.HelveticaBold });
        doc.Add(new TextBlock { Text = "Italic", FontName = StandardFonts.HelveticaOblique });
        string svg = doc.GenerateFirstPageSvg();

        Assert.DoesNotContain("font-family=\"Helvetica-Bold\"", svg, StringComparison.Ordinal);
        Assert.Contains("font-family=\"Helvetica, Arial, sans-serif\" font-weight=\"bold\"", svg, StringComparison.Ordinal);
        Assert.Contains("font-family=\"Helvetica, Arial, sans-serif\" font-style=\"italic\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void EmbeddedFont_EmitsSubsetFontFaceSmallerThanFullFont()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock
        {
            Text = "The quick brown fox jumps over the lazy dog 1234567890",
            FontName = TestFonts.SansRegularPath
        });

        SvgDocumentExport export = doc.GenerateSvgDocument();

        // Text references the private embedded family via an inline style (so a host
        // `* { font-family: inherit }` reset cannot override it); an @font-face data
        // URI binds the name to the subset bytes.
        Assert.Contains("style=\"font-family:'bpf-", export.Pages[0], StringComparison.Ordinal);
        Assert.Contains("@font-face", export.FontFaceCss, StringComparison.Ordinal);
        Assert.Contains("src:url(\"data:font/ttf;base64,", export.FontFaceCss, StringComparison.Ordinal);
        // The subset is a fraction of the full font (performance).
        long fullFontBytes = new System.IO.FileInfo(TestFonts.SansRegularPath).Length;
        Assert.True(export.FontFaceCss.Length < fullFontBytes / 2,
            $"Expected subset CSS ({export.FontFaceCss.Length}) to be far smaller than the full font ({fullFontBytes}).");
    }

    [Fact]
    public void StandardFontsOnly_EmitNoFontFaceCss()
    {
        // Standard-14 fonts are viewer-supplied, not embedded.
        SvgDocumentExport export = SimpleDoc().GenerateSvgDocument();
        Assert.Equal(string.Empty, export.FontFaceCss);
    }

    [Fact]
    public void ExternalHyperlink_EmitsSvgAnchorOverlay()
    {
        // Mirrors the PDF /Link annotation: external links become live SVG <a>
        // elements so hosts like @bonniepdf/previewer render clickable links.
        var doc = new BonniePDF.Document();
        doc.Add(new Hyperlink("https://www.bonniepdf.dev/docs", "Visit the docs"));

        string svg = doc.GenerateSvgPages()[0];
        Assert.Contains("<a href=\"https://www.bonniepdf.dev/docs\" target=\"_blank\" rel=\"noopener\"", svg, StringComparison.Ordinal);
        // The clickable area paints nothing but hit-tests across the link's bounds.
        Assert.Matches("<a [^>]*><rect [^>]*fill=\"none\" pointer-events=\"all\"/></a>", svg);
    }

    [Fact]
    public void ExternalHyperlink_NamesAnchorWithLinkText()
    {
        // The anchor carries an accessible name equal to the visible link text, so
        // a screen reader announces the same thing the PDF /Link annotation does
        // (parity with the annotation /Contents, PDF/UA 7-18).
        var doc = new BonniePDF.Document();
        doc.Add(new Hyperlink("https://www.bonniepdf.dev/docs", "Visit the docs"));

        string svg = doc.GenerateSvgPages()[0];
        Assert.Matches("<a href=\"[^\"]*\" target=\"_blank\" rel=\"noopener\" aria-label=\"Visit the docs\">", svg);
    }

    [Fact]
    public void HyperlinkUri_IsEscapedInOverlayMarkup()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new Hyperlink("https://example.com/?a=1&b=\"two\"", "Search"));

        string svg = doc.GenerateSvgPages()[0];
        Assert.Contains("href=\"https://example.com/?a=1&amp;b=&quot;two&quot;\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void HyperlinkAccessibleName_IsEscapedInAriaLabel()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new Hyperlink("https://example.com/terms", "Terms & \"Conditions\""));

        string svg = doc.GenerateSvgPages()[0];
        Assert.Contains("aria-label=\"Terms &amp; &quot;Conditions&quot;\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void PageWithLinks_IsLabelledImagePlusNamedSiblings_NotRoleImgOverLiveLinks()
    {
        // A page carrying focusable <a> links must NOT be an atomic image: ARIA
        // role="img" prunes its subtree, which both hides and invalidates the links.
        var doc = new BonniePDF.Document();
        doc.Add(new Hyperlink("https://www.bonniepdf.dev/docs", "Visit the docs"));

        string svg = doc.GenerateSvgPages()[0];
        // Root is a named graphics container, not an atomic image.
        Assert.Contains("aria-label=\"BonniePDF document - page 1\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"img\"", svg, StringComparison.Ordinal);
        // The painted page is the aria-hidden visual layer; its label comes from root.
        Assert.Contains("<g id=\"page-1\" data-bonniepdf-page=\"1\" aria-hidden=\"true\">", svg, StringComparison.Ordinal);
        // The link is a sibling of that group (outside it), not a pruned descendant.
        Assert.True(svg.IndexOf("<a ", StringComparison.Ordinal) > svg.IndexOf("</g>", StringComparison.Ordinal));
    }

    [Fact]
    public void LinkFreePage_KeepsAtomicImageModel()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Hello, SVG world!", FontSize = 14 });

        string svg = doc.GenerateSvgPages()[0];
        Assert.Contains("role=\"img\"", svg, StringComparison.Ordinal);
        Assert.Contains("<g id=\"page-1\" data-bonniepdf-page=\"1\">", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-hidden=\"true\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void InternalLink_EmitsPageMarkerRectInsteadOfAnchor()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new Hyperlink("Go to appendix") { TargetPageIndex = 1 }); // 0-based target -> 1-based marker
        doc.AddPage();
        doc.Add(new TextBlock { Text = "Appendix", FontSize = 11 });

        string svg = doc.GenerateSvgPages()[0];
        Assert.Contains("data-bp-link-page=\"2\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<a href", svg, StringComparison.Ordinal);
    }
}
