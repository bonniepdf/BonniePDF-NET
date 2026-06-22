using System;
using System.Collections.Generic;
using BonniePDF;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

public class SvgPageExportTests
{
    private static byte[] CreateMinimalJpeg() =>
    [
        0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03,
        0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00, 0xFF, 0xD9
    ];

    private static Document SimpleDoc()
    {
        var doc = new Document();
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
        var doc = new Document();
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
        var doc = new Document();
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
        var doc = new Document();
        doc.Add(new Image { ImageData = CreateMinimalJpeg(), Width = 64, AltText = "sample" });
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("<image", svg, StringComparison.Ordinal);
        Assert.Contains("data:image/jpeg;base64,", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Images_OmittedWhenRequested()
    {
        var doc = new Document();
        doc.Add(new Image { ImageData = CreateMinimalJpeg(), Width = 64, AltText = "sample" });
        string svg = doc.GenerateFirstPageSvg(new SvgPageExportOptions { EmbedImages = SvgImageEmbedding.Omit });
        Assert.DoesNotContain("<image", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Charts_RenderIntoPage()
    {
        var doc = new Document();
        var chart = new BarChart { Width = 400, Height = 250 };
        var data = new ChartData
        {
            Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }, new() { Label = "B", Value = 20 }] }]
        };
        doc.Add(new ChartElement(chart, data));
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("<rect", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Tables_Render()
    {
        var doc = new Document();
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
        doc.Settings.Watermark = new WatermarkSettings { Text = "DRAFT" };
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
        var doc = new Document();
        doc.Add(new TextBlock { Text = "Body" });
        doc.Settings.HeaderTemplate = pageNum => new TextBlock { Text = $"Header {pageNum}", FontSize = 9, TextColor = ColorPalette.Slate[500] };
        doc.Settings.FooterTemplate = pageNum => new TextBlock { Text = $"Footer {pageNum}", FontSize = 9, TextColor = ColorPalette.Slate[500] };
        string svg = doc.GenerateFirstPageSvg();
        Assert.Contains("Header 1", svg, StringComparison.Ordinal);
        Assert.Contains("Footer 1", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void OutOfRangePageIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimpleDoc().GenerateSvgPage(5));
    }

    [Fact]
    public void SpecialCharacters_AreEscaped()
    {
        var doc = new Document();
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
        var doc = new Document();
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
        var doc = new Document();
        doc.Add(new TextBlock
        {
            Text = "The quick brown fox jumps over the lazy dog 1234567890",
            FontName = TestFonts.SansRegularPath
        });

        SvgDocumentExport export = doc.GenerateSvgDocument();

        // Text references the private embedded family; an @font-face data URI binds it.
        Assert.Contains("font-family=\"bpf-", export.Pages[0], StringComparison.Ordinal);
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
}
