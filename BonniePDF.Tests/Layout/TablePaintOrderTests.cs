using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Table paint order: no cell fill is painted over a cell edge. A row paints its fills, then the bottom
/// edges held back from the row above, then its content, then its own edges; its bottom edges wait for the
/// next row's fills, or are drawn when the table leaves the page.
/// Mirrors core/tests/layout/table-paint-order.test.ts.
/// </summary>
public partial class TablePaintOrderTests
{
    private sealed record Rect(float X, float Y, float W, float H, int Order);

    private sealed record Page(List<Rect> Fills, List<Rect> Strokes);

    [GeneratedRegex(@"^([\d.]+) w$")]
    private static partial Regex WidthLine();

    [GeneratedRegex(@"^([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) re f$")]
    private static partial Regex FillLine();

    [GeneratedRegex(@"^([\d.-]+) ([\d.-]+) m ([\d.-]+) ([\d.-]+) l S$")]
    private static partial Regex SegmentLine();

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    /// <summary>Fills and strokes in paint order.</summary>
    private static Page PaintsOf(string stream)
    {
        var fills = new List<Rect>();
        var strokes = new List<Rect>();
        float width = 0;
        int order = 0;
        foreach (string raw in stream.Split('\n'))
        {
            string line = raw.Trim();
            Match w = WidthLine().Match(line);
            if (w.Success) width = F(w.Groups[1].Value);
            Match f = FillLine().Match(line);
            if (f.Success)
                fills.Add(new Rect(F(f.Groups[1].Value), F(f.Groups[2].Value), F(f.Groups[3].Value), F(f.Groups[4].Value), order++));
            Match s = SegmentLine().Match(line);
            if (s.Success)
            {
                float x1 = F(s.Groups[1].Value), y1 = F(s.Groups[2].Value), x2 = F(s.Groups[3].Value), y2 = F(s.Groups[4].Value);
                // The stroke's footprint: the line widened by half its width on each side.
                strokes.Add(y1 == y2
                    ? new Rect(Math.Min(x1, x2), y1 - width / 2, Math.Abs(x2 - x1), width, order++)
                    : new Rect(x1 - width / 2, Math.Min(y1, y2), width, Math.Abs(y2 - y1), order++));
            }
        }

        return new Page(fills, strokes);
    }

    private static bool Overlaps(Rect a, Rect b) =>
        Math.Min(a.X + a.W, b.X + b.W) - Math.Max(a.X, b.X) > 0.01f && Math.Min(a.Y + a.H, b.Y + b.H) - Math.Max(a.Y, b.Y) > 0.01f;

    /// <summary>Every fill that overlaps a stroke, painted after it.</summary>
    private static List<string> FillsOverStrokes(Page page) =>
        (from stroke in page.Strokes
         from fill in page.Fills
         where fill.Order > stroke.Order && Overlaps(fill, stroke)
         select $"fill {fill} over stroke {stroke}").ToList();

    private static Table ShadedTable(int rows)
    {
        Table table = new(2, 1, 1)
        {
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Color.FromHex("#94a3b8")),
            HeaderBackgroundColor = Color.FromHex("#1e293b"),
            HeaderTextColor = Color.White,
        };
        table.AddHeader(new TextBlock("Item"), new TextBlock("Qty"), new TextBlock("Price"));
        for (int i = 0; i < rows; i++)
        {
            TableRow row = new()
            {
                Cells =
                [
                    new TableCell { Content = new TextBlock($"Item {i + 1}") },
                    // A cell fill beside an unfilled cell, and a row fill on alternate rows.
                    new TableCell { Content = new TextBlock("1"), BackgroundColor = i % 3 == 0 ? Color.FromHex("#fde2e4") : null },
                    new TableCell { Content = new TextBlock("£9.00") },
                ],
            };
            if (i % 2 == 1) row.BackgroundColor = Color.FromHex("#f1f5f9");
            table.AddRow(row);
        }
        table.AddFooter(new TextBlock("Total"), new TextBlock(rows.ToString(CultureInfo.InvariantCulture)), new TextBlock("£99.00"));
        return table;
    }

    private static List<Page> Render(Table table)
    {
        using BonniePDF.Document doc = new(new DocumentSettings { Margin = 50 });
        doc.Add(table);
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).Select(p => PaintsOf(p.ContentStream)).ToList();
    }

    [Fact]
    public void PaintsNoFillOverAnEdge()
    {
        List<Page> pages = Render(ShadedTable(6));
        Assert.Single(pages);
        Assert.NotEmpty(pages[0].Fills);
        Assert.Empty(FillsOverStrokes(pages[0]));
    }

    [Fact]
    public void KeepsEveryEdgeVisible_WhenACellSpansColumns()
    {
        Table table = ShadedTable(2);
        table.AddRow(new TableRow
        {
            Cells =
            [
                new TableCell { Content = new TextBlock("Note"), ColumnSpan = 2, BackgroundColor = Color.FromHex("#fef3c7") },
                new TableCell { Content = new TextBlock("") },
            ],
        });
        List<Page> pages = Render(table);
        Assert.Empty(FillsOverStrokes(pages[0]));
    }

    [Fact]
    public void DrawsTheLastRowOnEachPage_WithItsBottomEdge_OnThatPage()
    {
        List<Page> pages = Render(ShadedTable(80));
        Assert.True(pages.Count > 1);
        foreach (Page page in pages)
        {
            Assert.Empty(FillsOverStrokes(page));
            // The lowest horizontal edge on the page closes the last row drawn there.
            float lowestFill = page.Fills.Min(f => f.Y);
            List<Rect> horizontal = page.Strokes.Where(s => s.W > s.H).ToList();
            float lowestEdge = horizontal.Min(s => s.Y + s.H / 2);
            Assert.True(lowestEdge <= lowestFill + 0.01f);
            // The page's last row is closed across its full width.
            float closed = horizontal.Where(s => Math.Abs(s.Y + s.H / 2 - lowestEdge) < 0.01f).Sum(s => s.W);
            Assert.Equal(495.28f, closed, 0.1f);
        }
    }
}
