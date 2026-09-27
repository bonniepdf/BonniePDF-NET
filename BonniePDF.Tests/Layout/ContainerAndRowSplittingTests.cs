using System.Text.RegularExpressions;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Padding, borders, backgrounds and rows are transparent to a splittable child's pagination: a table
/// inside them starts on the current page and continues on the next, instead of the whole box jumping to
/// a fresh page.
/// </summary>
public partial class ContainerAndRowSplittingTests
{
    private const int Rows = 60;

    [GeneratedRegex(@"\(r(\d+)c0\)")]
    private static partial Regex RowLabel();

    [GeneratedRegex(@"(?:^|\n)1 0 0 rg\n[\d.]+ [\d.]+ [\d.]+ [\d.]+ re")]
    private static partial Regex RedFill();

    // A horizontal border segment in black: "0 0 0 RG", a width, then "x1 y m x2 y l S".
    [GeneratedRegex(@"0 0 0 RG\n[\d.]+ w\n([\d.]+) ([\d.]+) m ([\d.]+) \2 l S")]
    private static partial Regex BlackHorizontal();

    // Horizontal black lines at least as wide as the box, on one page.
    private static int BlackHorizontals(string page) =>
        BlackHorizontal().Matches(page).Count(m => Math.Abs(float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture) - float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)) > 300);

    private static void RowsOfCells(BonniePDF.Fluent.Descriptors.TableDescriptor t)
    {
        t.Columns(1, 1);
        t.CellPadding(0);
        for (int r = 0; r < Rows; r++)
        {
            t.Cell().Text($"r{r}c0");
            t.Cell().Text($"r{r}c1");
        }
    }

    private static List<string> Pages(Action<IContentContainer> content)
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Margin(50);
            content(page.Content());
        })).Build();
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).Select(p => p.ContentStream).ToList();
    }

    private static List<int> RowsPerPage(List<string> pages) =>
        pages.Select(p => RowLabel().Matches(p).Count).ToList();

    [Fact]
    public void ATableInsidePaddingAndABorder_StartsOnTheFirstPageAndSplits()
    {
        List<string> pages = Pages(root => root.Column(c =>
        {
            c.Item().Text("HEADER");
            c.Item().PaddingTop(10).Border(1).Table(RowsOfCells);
        }));

        List<int> rows = RowsPerPage(pages);

        Assert.True(pages.Count >= 2);
        Assert.True(rows[0] > 0, "the table should start on page 1, not jump to page 2");
        Assert.Equal(Rows, rows.Sum()); // nothing lost, nothing repeated
    }

    [Fact]
    public void ATableInsideARow_StartsOnTheFirstPageAndSplits()
    {
        List<string> pages = Pages(root => root.Column(c =>
        {
            c.Item().Text("HEADER");
            c.Item().Row(row => row.RelativeItem().Table(RowsOfCells));
        }));

        List<int> rows = RowsPerPage(pages);

        Assert.True(rows[0] > 0);
        Assert.Equal(Rows, rows.Sum());
    }

    [Fact]
    public void ARowInsideAContainerInsideAColumn_Splits()
    {
        // The shape of a report page: padding, then a row, then a bordered table.
        List<string> pages = Pages(root => root.Column(c =>
        {
            c.Item().Text("HEADER");
            c.Item().PaddingTop(20).Row(row => row.RelativeItem().Border(1).Table(RowsOfCells));
        }));

        List<int> rows = RowsPerPage(pages);

        Assert.True(rows[0] > 0);
        Assert.Equal(Rows, rows.Sum());
    }

    [Fact]
    public void EachFragmentOfABackground_IsDrawnBehindItsContent()
    {
        List<string> pages = Pages(root => root.Column(c =>
        {
            c.Item().Text("HEADER");
            c.Item().Background(Color.Red).Table(RowsOfCells);
        }));

        Assert.True(pages.Count >= 2);
        foreach (string page in pages)
        {
            Match fill = RedFill().Match(page);
            Assert.True(fill.Success, "every page the box spans carries a fragment of its background");
            Assert.True(fill.Index < page.IndexOf("(r", StringComparison.Ordinal), "the background is drawn before the text, not over it");
        }
    }

    [Fact]
    public void ABoxWhoseChildCannotStartOnThePage_LeavesNoDecorationBehind()
    {
        // Fill most of page 1, leaving less than one table row, then a red box around a table: the box
        // must move to page 2 whole, with no red left behind on page 1.
        List<string> pages = Pages(root => root.Column(c =>
        {
            for (int i = 0; i < 51; i++) c.Item().Text($"filler {i}"); // 51 lines of 14.4 pt leave 7.5 pt: less than one table row
            c.Item().Background(Color.Red).Table(RowsOfCells);
        }));

        Assert.True(pages.Count >= 2);
        Assert.DoesNotMatch(RedFill(), pages[0]);
        Assert.Matches(RedFill(), pages[1]);
    }

    [Fact]
    public void AContainerThatFits_IsDrawnWholeAsBefore()
    {
        List<string> pages = Pages(root => root.Column(c =>
        {
            c.Item().Background(Color.Red).PaddingVertical(5).Table(t =>
            {
                t.Columns(1, 1);
                t.CellPadding(0);
                for (int r = 0; r < 3; r++)
                {
                    t.Cell().Text($"r{r}c0");
                    t.Cell().Text($"r{r}c1");
                }
            });
        }));

        Assert.Single(pages);
        Assert.Equal(3, RowsPerPage(pages)[0]);
    }

    [Fact]
    public void ASplitTablesFrame_IsACompleteRectangleOnEveryPage()
    {
        List<string> pages = Pages(root => root.Column(c =>
        {
            c.Item().Text("HEADER");
            c.Item().Border(1).Table(RowsOfCells);
        }));

        Assert.True(pages.Count >= 2);

        // The bottom edge under the last row on each page, and the top edge over the first: two lines each.
        Assert.All(pages, page => Assert.Equal(2, BlackHorizontals(page)));
    }

    [Fact]
    public void AnOrdinaryBorderedBox_StillSlicesItsBorderAcrossPages()
    {
        List<string> pages = Pages(root => root.Border(1).Column(c =>
        {
            for (int i = 0; i < 90; i++) c.Item().Text($"line {i}");
        }));

        Assert.True(pages.Count >= 2);

        // Not a table frame: top on the first fragment only, bottom on the last only.
        Assert.Equal(1, BlackHorizontals(pages[0]));
        Assert.Equal(1, BlackHorizontals(pages[^1]));
    }
}
