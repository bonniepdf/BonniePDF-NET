using System.Globalization;
using System.Text.RegularExpressions;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// A border or background applied to a table cell's content spans the whole row, not just the cell's
/// own text: a short cell beside a tall one is drawn as tall as the tall one.
/// </summary>
public partial class TableCellStretchTests
{
    // A vertical border segment: "x y1 m x y2 l S".
    [GeneratedRegex(@"([\d.]+) ([\d.]+) m \1 ([\d.]+) l S")]
    private static partial Regex VerticalEdge();

    // A filled rectangle: "x y w h re" followed by a fill operator.
    [GeneratedRegex(@"([\d.]+) ([\d.]+) ([\d.]+) ([\d.]+) re\s*f")]
    private static partial Regex FilledRect();

    private static string FirstRowStream(Action<BonniePDF.Fluent.Descriptors.TableDescriptor> table)
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Margin(50);
            page.Content().Table(table);
        })).Build();
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).First().ContentStream;
    }

    private const string Long = "a much longer piece of text that has to wrap onto several lines inside its narrow column";

    [Fact]
    public void CellBorders_SpanTheRowHeight()
    {
        string stream = FirstRowStream(t =>
        {
            t.Columns(1, 1, 1);
            t.CellPadding(0);
            t.Cell().Border(1).PaddingVertical(4).Text("short");
            t.Cell().Border(1).PaddingVertical(4).Text(Long);
            t.Cell().Border(1).PaddingVertical(4).Text("");
        });

        // The left edge of each of the three cells: same top, same length.
        List<float> heights = VerticalEdge().Matches(stream)
            .Select(m => Math.Abs(float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) - float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)))
            .Where(h => h > 1)
            .ToList();

        Assert.NotEmpty(heights);
        Assert.All(heights, h => Assert.Equal(heights[0], h, 2));
        Assert.True(heights[0] > 40, $"expected a tall row (the wrapped cell), got {heights[0]}");
    }

    [Fact]
    public void CellBackgrounds_SpanTheRowHeight()
    {
        string stream = FirstRowStream(t =>
        {
            t.Columns(1, 1);
            t.CellPadding(0);
            t.Cell().Background(Color.Red).PaddingVertical(4).Text("short");
            t.Cell().Background(Color.Blue).PaddingVertical(4).Text(Long);
        });

        List<float> heights = FilledRect().Matches(stream)
            .Select(m => float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture))
            .ToList();

        Assert.Equal(2, heights.Count);
        Assert.Equal(heights[0], heights[1], 2);
    }

    [Fact]
    public void ANestedContainerInsideACell_KeepsItsNaturalHeight()
    {
        string stream = FirstRowStream(t =>
        {
            t.Columns(1, 1);
            t.CellPadding(0);
            t.Cell().Column(c => c.Item().Background(Color.Red).Text("short"));
            t.Cell().Text(Long);
        });

        float redHeight = FilledRect().Match(stream).Groups[4].Success
            ? float.Parse(FilledRect().Match(stream).Groups[4].Value, CultureInfo.InvariantCulture)
            : 0;

        // The red block is inside a column inside the cell, so it is not the cell's outermost container
        // and stays one line tall.
        Assert.InRange(redHeight, 1, 20);
    }
}
