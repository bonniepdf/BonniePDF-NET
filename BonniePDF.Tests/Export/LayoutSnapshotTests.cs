using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Export;

// Mirrors the TypeScript suite tests/export/layout-snapshot.test.ts and the
// shared fixture spec/fixtures/layout-snapshot/vertical-stack.json. Verifies the
// layout snapshot data layer behind the previewer structure view (spec 07).
//
// NOTE: written without a .NET toolchain in the authoring environment; run this
// before considering the .NET mirror landed.
public class LayoutSnapshotTests
{
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
        doc.Add(new BonniePDF.Core.Hyperlink("https://example.com", "Visit example"));

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
}
