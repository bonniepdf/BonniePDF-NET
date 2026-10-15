using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Layout;

// A row added as plain content is kept compact (TableRowStore). These pin that callers cannot
// tell: the same output, the same row object on every read, and changes through it kept.
public sealed partial class CompactTableRowTests
{
    private static byte[] Generate(Action<Table> fill, Action<Table>? afterFill = null)
    {
        var document = new BonniePDF.Document();
        document.Metadata.CreationDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        document.Metadata.ModifiedDate = document.Metadata.CreationDate;
        var table = new Table(2, 1) { CellPadding = 2, RepeatHeadersOnNewPage = true };
        table.AddHeader(new TextBlock("Item"), new TextBlock("Qty"));
        fill(table);
        afterFill?.Invoke(table);
        document.Add(table);
        using var stream = new MemoryStream();
        document.Save(stream, leaveOpen: true);
        return stream.ToArray();
    }

    private static string Normalize(byte[] bytes) =>
        FileIdentifierRegex().Replace(Encoding.Latin1.GetString(bytes), "/ID [<MASKED> <MASKED>]");

    [GeneratedRegex(@"/ID \[<[0-9A-F]*> <[0-9A-F]*>\]")]
    private static partial Regex FileIdentifierRegex();

    private static bool IsCompact(Table table, int index)
    {
        var store = typeof(Table).GetField("_rows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(table)!;
        return (bool)store.GetType().GetMethod("IsCompact", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, [index])!;
    }

    [Fact]
    public void PlainRowsRenderAsTheEquivalentExplicitRows()
    {
        // Enough rows to break across pages, so repeated headers and continuation are covered.
        byte[] compact = Generate(t => { for (int i = 0; i < 200; i++) t.AddRow(new TextBlock($"Row {i}"), new TextBlock($"{i}")); });
        byte[] explicitRows = Generate(t => { for (int i = 0; i < 200; i++) t.AddRow(TableRow.From(new TextBlock($"Row {i}"), new TextBlock($"{i}"))); });
        Assert.Equal(Normalize(explicitRows), Normalize(compact));
    }

    [Fact]
    public void AReadRowIsKeptAndItsChangesStay()
    {
        var table = new Table(1);
        table.AddRow(new TextBlock("a"));
        Assert.True(IsCompact(table, 0));
        TableRow first = table.Rows[0];
        Assert.Same(first, table.Rows[0]);
        Assert.Same(first, table.GetAllRows()[1 - 1]);
        Assert.False(IsCompact(table, 0));
        Assert.Equal("a", ((TextBlock)first.Cells[0].Content).Text);
        Assert.Equal(0, table.Rows.IndexOf(first));
        Assert.True(table.Rows.Remove(first));
        Assert.Empty(table.Rows);
    }

    [Fact]
    public void ChangesThroughAReadRowReachTheOutput()
    {
        byte[] coloured = Generate(t => t.AddRow(new TextBlock("x"), new TextBlock("1")), t => t.Rows[0].WithBackground(Color.FromHex("#ff0000")));
        byte[] plain = Generate(t => t.AddRow(new TextBlock("x"), new TextBlock("1")));
        Assert.NotEqual(Normalize(plain), Normalize(coloured));
        byte[] explicitColoured = Generate(t => t.AddRow(TableRow.From(new TextBlock("x"), new TextBlock("1")).WithBackground(Color.FromHex("#ff0000"))));
        Assert.Equal(Normalize(explicitColoured), Normalize(coloured));
    }

    [Fact]
    public void ChangingTheCallersArrayAfterAddingHasNoEffect()
    {
        var table = new Table(2);
        var cells = new IElement[] { new TextBlock("a"), new TextBlock("b") };
        table.AddRow(cells);
        cells[0] = new TextBlock("changed");
        Assert.Equal("a", ((TextBlock)table.Rows[0].Cells[0].Content).Text);
    }

    [Fact]
    public void TreeWalksDoNotMaterialiseRows()
    {
        var table = new Table(2);
        var a = new TextBlock("a");
        var b = new TextBlock("b");
        table.AddRow(a, b);
        table.AddRow(new TextBlock("c"));
        Assert.Equal([a, b], table.GetChildren().Take(2));
        Assert.Equal(3, table.GetChildren().Count());
        // Saving walks the tree (fonts, styles, direction) and lays the table out.
        var document = new BonniePDF.Document();
        document.Add(table);
        document.GenerateBytes();
        Assert.True(IsCompact(table, 0));
        Assert.True(IsCompact(table, 1));
    }

    [Fact]
    public void RowsWithACallersCellAreKeptAsGiven()
    {
        var table = new Table(2);
        var cell = new TableCell { Content = new TextBlock("mine") };
        table.AddRow(cell, new TextBlock("b"));
        Assert.False(IsCompact(table, 0));
        Assert.Same(cell, table.Rows[0].Cells[0]);
    }

    [Fact]
    public void NullRowsAndNullContentBehaveAsBefore()
    {
        var table = new Table(1);
        table.Rows.Add(null!);
        Assert.Null(table.Rows[0]);
        table.AddRow((IElement)null!);
        Assert.Null(table.Rows[1].Cells[0].Content);
    }
}
