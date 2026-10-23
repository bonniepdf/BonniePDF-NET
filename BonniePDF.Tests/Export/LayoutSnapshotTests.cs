using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Export;

// Mirrors the TypeScript suite tests/export/layout-snapshot.test.ts and the
// shared fixture spec/fixtures/layout-snapshot/vertical-stack.json. Verifies the
// layout snapshot data layer behind the previewer structure view.
//
// NOTE: written without a .NET toolchain in the authoring environment; run this
// before considering the .NET mirror landed.
public partial class LayoutSnapshotTests
{
    [Theory]
    [InlineData("Barcode", "#ffffff")]
    [InlineData("QRCode", "#ffffff")]
    [InlineData("DataMatrix", "#ffffff")]
    [InlineData("BarChart", "#fafafa")]
    public void CapturesSymbolAndChartBackgrounds(string type, string expected)
    {
        object element = type switch
        {
            "Barcode" => new Barcode(),
            "QRCode" => new QRCode(),
            "DataMatrix" => new DataMatrix(),
            _ => new BarChart { BackgroundColor = Color.FromHex("#fafafa") }
        };
        Assert.Equal(expected, LayoutRecorder.PaintOf(element, type)?.Background);
    }

    [GeneratedRegex(@"/ID \[[^\]]*\]|/(?:CreationDate|ModDate) \([^)]*\)")]
    private static partial Regex TrailerId();

    private static IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode d in Walk(child))
                yield return d;
    }

    [Fact]
    public void NoSnapshot_WhenCaptureLayoutNotRequested()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Hello", FontSize = 14 });
        SvgDocumentExport export = doc.GenerateSvgDocument();
        Assert.Null(export.Snapshot);
    }

    [Fact]
    public void Captures_Document_Page_Content_Tree()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "First", FontSize = 14 });
        doc.Add(new TextBlock { Text = "Second", FontSize = 11 });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;

        Assert.NotNull(snapshot);
        Assert.Equal(LayoutSnapshotSchema.Version, snapshot.Version);
        Assert.Single(snapshot.Pages);

        LayoutNode root = snapshot.Root;
        Assert.Equal("Document", root.Type);
        Assert.Single(root.Children);

        LayoutNode page = root.Children[0];
        Assert.Equal("Page", page.Type);
        Assert.Equal(new[] { "TextBlock", "TextBlock" }, page.Children.Select(c => c.Type).ToArray());
    }

    [Fact]
    public void Rectangles_AreTopLeft_InBounds_Points()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Measure me", FontSize = 14 });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        LayoutSnapshot.PageSize size = snapshot.Pages[0];

        LayoutNode text = Walk(snapshot.Root).First(n => n.Type == "TextBlock");
        Assert.NotEmpty(text.Rects);
        LayoutRect r = text.Rects[0];
        Assert.Equal(0, r.Page);
        Assert.True(r.X >= 0);
        Assert.True(r.Y >= 0);
        Assert.True(r.Y < size.Height / 2); // near the top (top-left origin)
        Assert.True(r.X + r.Width <= size.Width + 0.5f);
    }

    [Fact]
    public void Nests_Children_UnderVerticalStack()
    {
        var doc = new BonniePDF.Document();
        var stack = new VerticalStack();
        stack.Add(new TextBlock { Text = "A", FontSize = 12 });
        stack.Add(new TextBlock { Text = "B", FontSize = 12 });
        doc.Add(stack);

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        LayoutNode vstack = Walk(snapshot.Root).First(n => n.Type == "VerticalStack");
        Assert.Equal(new[] { "TextBlock", "TextBlock" }, vstack.Children.Select(c => c.Type).ToArray());
    }

    [Fact]
    public void Captures_HyperlinkNodes_WithLinkRole()
    {
        // Hyperlink implements IElement directly (not ElementBase), so it records
        // its own snapshot node - a regression here makes links invisible to the
        // previewer's structure and accessibility views.
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Before", FontSize = 12 });
        doc.Add(new BonniePDF.Hyperlink("https://example.com", "Visit example"));

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        LayoutNode link = Walk(snapshot.Root).First(n => n.Type == "Hyperlink");
        Assert.NotNull(link.A11y);
        Assert.Equal("Link", link.A11y!.Role);
        Assert.True(link.A11y.Tagged);
        Assert.NotEmpty(link.Rects);
    }

    [Fact]
    public void Splits_PaginatingElement_OneRectPerPage()
    {
        var doc = new BonniePDF.Document();
        for (int i = 0; i < 120; i++)
            doc.Add(new TextBlock { Text = $"Line {i + 1}", FontSize = 14 });

        SvgDocumentExport export = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true });
        Assert.True(export.Pages.Count > 1);
        Assert.Equal(export.Pages.Count, export.Snapshot.Pages.Count);

        LayoutNode page = export.Snapshot.Root.Children[0];
        var touched = page.Rects.Select(r => r.Page).Distinct().ToList();
        Assert.Equal(export.Pages.Count, touched.Count);
        Assert.All(page.Rects, r =>
        {
            Assert.InRange(r.Page, 0, export.Pages.Count - 1);
            Assert.True(r.Height > 0);
        });
    }

    [Fact]
    public void NodeIds_AreUnique_AndDeterministic()
    {
        BonniePDF.Document Build()
        {
            var d = new BonniePDF.Document();
            d.Add(new TextBlock { Text = "X", FontSize = 12 });
            d.Add(new TextBlock { Text = "Y", FontSize = 12 });
            return d;
        }

        LayoutSnapshot a = Build().GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        LayoutSnapshot b = Build().GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;

        var idsA = Walk(a.Root).Select(n => n.Id).ToList();
        var idsB = Walk(b.Root).Select(n => n.Id).ToList();
        Assert.Equal(idsA.Count, idsA.Distinct().Count()); // unique
        Assert.Equal(idsA, idsB);                          // deterministic
    }

    [Fact]
    public void ByteUnchangedGuard_CaptureLayoutDoesNotAlterSvg()
    {
        BonniePDF.Document Build()
        {
            var d = new BonniePDF.Document();
            var stack = new VerticalStack();
            stack.Add(new TextBlock { Text = "Alpha", FontSize = 14 });
            stack.Add(new TextBlock { Text = "Beta", FontSize = 11 });
            d.Add(stack);
            for (int i = 0; i < 60; i++) d.Add(new TextBlock { Text = $"Row {i}", FontSize = 12 });
            return d;
        }

        SvgDocumentExport off = Build().GenerateSvgDocument();
        SvgDocumentExport on = Build().GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true });

        Assert.Equal(off.Pages, on.Pages);
        Assert.Equal(off.FontFaceCss, on.FontFaceCss);
    }

    [Fact]
    public void CarriesOpaqueSourceIds_OnlyForMarkedElements()
    {
        var doc = new BonniePDF.Document();
        // The innermost mark wins: an outer call returning the same element does not replace it.
        TextBlock marked = DebugSource.Mark(DebugSource.Mark(new TextBlock { Text = "Marked", FontSize = 14 }, "src:test-title"), "src:outer-factory");
        doc.Add(marked);
        doc.Add(new TextBlock { Text = "Unmarked", FontSize = 11 });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot!;

        List<LayoutNode> nodes = Walk(snapshot.Root).Where(n => n.Type == "TextBlock").ToList();
        Assert.Equal("src:test-title", nodes[0].SourceId);
        Assert.Null(nodes[1].SourceId);
    }

    [Fact]
    public void IgnoresAnEmptySourceId()
    {
        var doc = new BonniePDF.Document();
        doc.Add(DebugSource.Mark(new TextBlock { Text = "Empty", FontSize = 14 }, ""));

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot!;

        Assert.Null(Walk(snapshot.Root).Single(n => n.Type == "TextBlock").SourceId);
    }

    [Fact]
    public void MapsTheDocumentRootAndItsAutoCreatedPage_ToTheDocumentDeclaration()
    {
        BonniePDF.Document doc = DebugSource.Mark(new BonniePDF.Document(), "src:document-declaration");
        doc.Add(new TextBlock { Text = "First page", FontSize = 14 });
        doc.AddPage().Add(new TextBlock { Text = "Second page", FontSize = 14 });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot!;

        Assert.Equal("src:document-declaration", snapshot.Root.SourceId);
        Assert.Equal("src:document-declaration", snapshot.Root.Children[0].SourceId);
        Assert.Null(snapshot.Root.Children[1].SourceId);
    }

    [Fact]
    public void SourceIds_NeverReachThePdfOrTheSvg()
    {
        static (byte[] Pdf, string Svg) Render(bool marked)
        {
            var doc = new BonniePDF.Document();
            TextBlock text = new() { Text = "Same either way", FontSize = 14 };
            doc.Add(marked ? DebugSource.Mark(text, "src:only-in-the-snapshot") : text);
            if (marked) DebugSource.Mark(doc, "src:document");
            string svg = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Pages[0];
            return (doc.GenerateBytes(), svg);
        }

        // The trailer's /ID is random per generation; nothing else may differ.
        static string MaskId(byte[] pdf) => TrailerId().Replace(System.Text.Encoding.Latin1.GetString(pdf), "/ID []");

        (byte[] pdfA, string svgA) = Render(false);
        (byte[] pdfB, string svgB) = Render(true);

        Assert.Equal(MaskId(pdfA), MaskId(pdfB));
        Assert.Equal(svgA, svgB);
        Assert.DoesNotContain("src:", svgB);
    }
}
