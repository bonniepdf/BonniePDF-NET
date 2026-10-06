using System.Globalization;
using System.Text.RegularExpressions;
using BonniePDF.Fluent;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Table borders: the table's own border applies to every cell, a shared edge between two cells is drawn
/// once (the wider line wins, then the darker, and a tie goes to the earlier cell), and a bordered container
/// around a table is the table's frame, painted over its outer edge.
/// </summary>
public partial class TableBorderCollapseTests
{
    private sealed record Stroke(string Colour, float Width, float X1, float Y1, float X2, float Y2, int Order);

    [GeneratedRegex(@"^([\d.]+ [\d.]+ [\d.]+) RG$")]
    private static partial Regex ColourLine();

    [GeneratedRegex(@"^([\d.]+) w$")]
    private static partial Regex WidthLine();

    [GeneratedRegex(@"^([\d.]+) ([\d.]+) m ([\d.]+) ([\d.]+) l S$")]
    private static partial Regex SegmentLine();

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    private static List<Stroke> Strokes(string stream)
    {
        var strokes = new List<Stroke>();
        string colour = "";
        float width = 0;
        foreach (string raw in stream.Split('\n'))
        {
            string line = raw.Trim();
            Match c = ColourLine().Match(line);
            if (c.Success) { colour = c.Groups[1].Value; continue; }
            Match w = WidthLine().Match(line);
            if (w.Success) { width = F(w.Groups[1].Value); continue; }
            Match s = SegmentLine().Match(line);
            if (s.Success)
                strokes.Add(new Stroke(colour, width, F(s.Groups[1].Value), F(s.Groups[2].Value), F(s.Groups[3].Value), F(s.Groups[4].Value), strokes.Count));
        }

        return strokes;
    }

    private static string Render(Action<IContentContainer> content)
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Margin(50);
            content(page.Content());
        })).Build();
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).First().ContentStream;
    }

    private static string RenderTable(Table table)
    {
        using BonniePDF.Document doc = new();
        doc.Add(table);
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).First().ContentStream;
    }

    private static TableCell Cell(string text, Color colour, float width) =>
        TableCell.From(new TextBlock { Text = text }).WithBorder(colour, width);

    // Segments on the vertical line x = at, whichever cell drew them.
    private static List<Stroke> OnVertical(List<Stroke> strokes, float at) =>
        strokes.Where(s => Math.Abs(s.X1 - at) < 0.01f && Math.Abs(s.X2 - at) < 0.01f).ToList();

    [Fact]
    public void TheTablesOwnBorder_AppliesToEveryCell()
    {
        string stream = Render(root => root.Table(t =>
        {
            t.Columns(1, 1);
            t.Border(1, Color.Red);
            for (int i = 0; i < 4; i++) t.Cell().Text("x" + i);
        }));

        Assert.NotEmpty(Strokes(stream)); // it used to draw nothing at all
    }

    [Fact]
    public void ASharedEdge_IsDrawnOnce()
    {
        string stream = Render(root => root.Table(t =>
        {
            t.Columns(1, 1);
            t.CellPadding(0);
            t.Border(1, Color.Black);
            for (int i = 0; i < 4; i++) t.Cell().Text("x" + i);
        }));

        List<Stroke> strokes = Strokes(stream);
        var distinct = strokes.Select(s => (s.X1, s.Y1, s.X2, s.Y2)).Distinct().ToList();

        // A 2 x 2 grid has twelve unit edges (3 vertical lines and 3 horizontal lines, two cells long each).
        Assert.Equal(12, strokes.Count);
        Assert.Equal(strokes.Count, distinct.Count);
    }

    [Fact]
    public void TheWiderLineWinsASharedEdge_WhenTheLaterCellHasIt()
    {
        var table = new Table(1, 1);
        table.AddRow(Cell("a", Color.Black, 1), Cell("b", Color.Red, 3));

        List<Stroke> strokes = Strokes(RenderTable(table));
        float shared = strokes.Where(s => s.Colour == "0 0 0").Select(s => s.X1).Distinct().Order().ToList()[^1];
        List<Stroke> atShared = OnVertical(strokes, shared);

        // The left cell draws its right edge; the right cell's wider one is painted after it, over it.
        Assert.Equal(new[] { "0 0 0", "1 0 0" }, atShared.Select(s => s.Colour).ToArray());
        Assert.Equal(3, atShared[^1].Width);
    }

    [Fact]
    public void TheWiderLineWinsASharedEdge_WhenTheEarlierCellHasIt()
    {
        var table = new Table(1, 1);
        table.AddRow(Cell("a", Color.Red, 3), Cell("b", Color.Black, 1));

        List<Stroke> strokes = Strokes(RenderTable(table));
        float shared = strokes.Where(s => s.Colour == "1 0 0").Select(s => s.X1).Distinct().Order().ToList()[^1];
        List<Stroke> atShared = OnVertical(strokes, shared);

        // The narrower right cell does not draw its left edge at all.
        Assert.Single(atShared);
        Assert.Equal("1 0 0", atShared[0].Colour);
    }

    [Fact]
    public void TheDarkerLineWinsATie_OtherwiseTheEarlierCell()
    {
        var darker = new Table(1, 1);
        darker.AddRow(Cell("a", Color.FromHex("#999999"), 1), Cell("b", Color.Black, 1));
        var lighter = new Table(1, 1);
        lighter.AddRow(Cell("a", Color.Black, 1), Cell("b", Color.FromHex("#999999"), 1));

        List<Stroke> sDarker = Strokes(RenderTable(darker));
        List<Stroke> sLighter = Strokes(RenderTable(lighter));

        // Later cell darker: it paints over the shared edge, so black is the last stroke there.
        float xDarker = sDarker.Where(s => s.Colour == "0 0 0").Select(s => s.X1).Min();
        Assert.Equal("0 0 0", OnVertical(sDarker, xDarker)[^1].Colour);

        // Later cell lighter: it does not draw, so only the earlier black one is there.
        float xLighter = sLighter.Where(s => s.Colour == "0 0 0").Select(s => s.X1).Max();
        Assert.Single(OnVertical(sLighter, xLighter));
    }

    [Fact]
    public void RowsCollapseToo_TheLowerCellDoesNotRedrawTheEdgeAbove()
    {
        var table = new Table(1);
        table.AddRow(Cell("a", Color.Black, 1));
        table.AddRow(Cell("b", Color.Black, 1));

        List<Stroke> strokes = Strokes(RenderTable(table));

        // Two stacked cells: three horizontal lines, not four.
        int horizontals = strokes.Count(s => Math.Abs(s.Y1 - s.Y2) < 0.01f);
        Assert.Equal(3, horizontals);
    }

    [Fact]
    public void ABorderedContainerAroundATable_IsItsFrame_PaintedOverTheOuterEdge()
    {
        string stream = Render(root => root.Border(1).Table(t =>
        {
            t.Columns(1, 1);
            t.CellPadding(0);
            t.Border(1, Color.FromHex("#999999"));
            for (int i = 0; i < 4; i++) t.Cell().Text("x" + i);
        }));

        List<Stroke> strokes = Strokes(stream);
        List<Stroke> frame = strokes.Where(s => s.Colour == "0 0 0").ToList();
        List<Stroke> grid = strokes.Where(s => s.Colour != "0 0 0").ToList();

        Assert.NotEmpty(frame);
        Assert.NotEmpty(grid);

        // The frame sits exactly on the outermost cell edges, not beside them, and is painted after them.
        float left = frame.Min(s => Math.Min(s.X1, s.X2));
        Assert.Equal(left, grid.Min(s => Math.Min(s.X1, s.X2)), 3);
        Assert.True(frame.Min(s => s.Order) > grid.Max(s => s.Order), "the frame is painted over the table");
    }

    [Fact]
    public void AnotherBorderedContainer_StillInsetsItsChild()
    {
        string stream = Render(root => root.Border(1).Text("text in a box"));

        // Nothing to draw over, and the border keeps the box model: a stroke, and the text pushed in by it.
        Assert.NotEmpty(Strokes(stream));
        Assert.Contains("1 0 0 1 51 ", stream); // x = 50 margin + 1 border
    }
}
