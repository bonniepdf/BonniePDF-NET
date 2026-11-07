using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Column spans, wrapping, aligned numbers, repeated headers, and empty data.</summary>
[System.ComponentModel.DisplayName("Tables with Real-World Content")]
[System.ComponentModel.Category("Tables & Lists")]
[System.ComponentModel.Description("Column spans, wrapping descriptions, aligned numbers, repeated headers, and empty results.")]
public static class PracticalTablesExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings { Language = "en-GB", Margin = 40 }) { Metadata = new DocumentMetadata { Title = "Tables with real-world content" } };
        doc.Add(new Heading("Tables with real-world content") { Level = 1, FontSize = 23 });
        doc.Add(new TextBlock("The description column has most of the available width. Numbers align right, headers repeat, and a spanning row labels each section.") { Margin = new Thickness(0, 0, 0, 12) });
        doc.Add(BuildTable(Enumerable.Range(0, 36).Select(i => new Item($"ITEM-{i + 1:000}",
            i == 17 ? "Long description: " + string.Concat(Enumerable.Repeat("Installation, commissioning and handover documentation. ", 14)) : $"Replacement component {i + 1}, with delivery and installation notes that wrap naturally.",
            i % 3 + 1, (i + 1) * 125)).ToArray()));
        doc.Add(new PageBreak());
        doc.Add(new Heading("Empty results") { Level = 2, FontSize = 18 });
        doc.Add(BuildTable([]));
        doc.Add(new Heading("A fixed-width table frame") { Level = 2, FontSize = 18, Margin = new Thickness(0, 16, 0, 0) });
        doc.Add(new TextBlock("The frame is 360 points wide; its columns still share that space using relative weights."));
        doc.Add(new Container { Width = 360, Child = BuildTable([new Item("FIXED-001", "A table inside a fixed-width frame.", 1, 1250)]) });
        return doc;
    }

    private sealed record Item(string Id, string Description, int Quantity, int Amount);

    private static Table BuildTable(Item[] items)
    {
        // Relative weights, not fixed pixel widths.
        var table = new Table(1.3f, 5, 0.7f, 1.2f) { CellPadding = 7, RepeatHeadersOnNewPage = true };
        table.AddHeader("Item", "Description", "Qty", "Amount");
        table.AddRow(new TableRow { Cells = [new TableCell { Content = new TextBlock("Order detail"), ColumnSpan = 4 }] });
        if (items.Length == 0) table.AddRow(new TableRow { Cells = [new TableCell { Content = new TextBlock("No items match this selection."), ColumnSpan = 4 }] });
        foreach (var item in items) table.AddRow(new TextBlock(item.Id) { FontSize = 10 }, new TextBlock(item.Description) { FontSize = 10 },
            new TextBlock(item.Quantity.ToString(CultureInfo.InvariantCulture)) { Alignment = TextAlignment.Right, FontSize = 10 },
            new TextBlock((item.Amount / 100m).ToString("F2", CultureInfo.InvariantCulture)) { Alignment = TextAlignment.Right, FontSize = 10 });
        return table;
    }
}
