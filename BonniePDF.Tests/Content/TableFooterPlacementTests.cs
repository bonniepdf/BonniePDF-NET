using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>Spec 03, Table footers: the footer follows the last body row wherever the table is placed.</summary>
[Collection("Serial")]
public sealed class TableFooterPlacementTests
{
    private static Table Build()
    {
        Table table = new(1, 1);
        table.AddHeader("Item", "Amount");
        table.AddRow("Widget", "12.00");
        table.AddFooter("Total", "12.00");
        return table;
    }

    private static bool DrawsFooter(Func<Table, IElement> wrap)
    {
        using Document doc = new();
        doc.Add(wrap(Build()));
        return doc.GenerateSvgPages()[0].Contains(">Total<", StringComparison.Ordinal);
    }

    [Fact]
    public void TableOnThePage_DrawsItsFooter() => Assert.True(DrawsFooter(table => table));

    [Fact]
    public void TableInsideAContainer_DrawsItsFooter() =>
        Assert.True(DrawsFooter(table => new Container { Width = 300, Child = table }));

    [Fact]
    public void TableInsideAStack_DrawsItsFooter() =>
        Assert.True(DrawsFooter(table =>
        {
            VerticalStack stack = new();
            stack.Add(table);
            return stack;
        }));
}
