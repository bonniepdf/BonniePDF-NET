using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>A styled data table: banded rows, column spans, wrapping, aligned numbers, repeated headers, a subtotal carried across page breaks, a totals footer, and empty data.</summary>
[System.ComponentModel.DisplayName("Tables with Real-World Content")]
[System.ComponentModel.Category("Tables & Lists")]
[System.ComponentModel.Description("A styled data table: banded rows, column spans, wrapping descriptions, aligned numbers, repeated headers, a subtotal carried across page breaks, a totals footer, and empty results.")]
public static class PracticalTablesExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Margin = 40,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[700]),
        })
        { Metadata = new DocumentMetadata { Title = "Tables with real-world content" } };
        doc.Add(new Heading("Tables with real-world content") { Level = 1, FontSize = 23 });
        doc.Add(new TextBlock("The description column has most of the available width. Numbers align right, headers repeat, a spanning row labels each section, and the amount subtotal is carried from page to page.") { Margin = new Thickness(0, 0, 0, 12) });
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
        // Relative weights, not fixed pixel widths. When the table breaks across pages it carries its
        // amount subtotal forward, and the footer totals the whole table.
        var table = new Table(1.3f, 5, 0.7f, 1.2f)
        {
            CellPadding = 7,
            RepeatHeadersOnNewPage = true,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            HeaderBackgroundColor = ColorPalette.Slate[800],
            HeaderTextColor = Color.White,
            FooterBackgroundColor = ColorPalette.Slate[100],
            FooterTextColor = ColorPalette.Slate[900],
            RunningTotalColumn = 3,
        };
        string[] labels = ["Item", "Description", "Qty", "Amount"];
        table.AddHeader(labels.Select((label, i) => (IElement)new TextBlock(label)
            { FontWeight = FontWeight.Bold, Alignment = i >= 2 ? TextAlignment.Right : TextAlignment.Left }).ToArray());
        table.AddRow(new TableRow
        {
            Cells = [new TableCell { Content = new TextBlock("Order detail") { FontWeight = FontWeight.Bold }, ColumnSpan = 4 }],
            BackgroundColor = ColorPalette.Slate[100],
        });
        if (items.Length == 0)
            table.AddRow(new TableRow
            {
                Cells = [new TableCell { Content = new TextBlock("No items match this selection.") { Italic = true, Color = ColorPalette.Slate[500] }, ColumnSpan = 4 }],
            });
        for (int i = 0; i < items.Length; i++)
        {
            Item item = items[i];
            IElement[] cells =
            [
                new TextBlock(item.Id),
                new TextBlock(item.Description),
                new TextBlock(item.Quantity.ToString(CultureInfo.InvariantCulture)) { Alignment = TextAlignment.Right },
                new TextBlock(Money(item.Amount)) { Alignment = TextAlignment.Right },
            ];
            table.AddRow(new TableRow
            {
                Cells = cells.Select(content => new TableCell { Content = content }).ToArray(),
                // Every other row is banded, so a long row is easy to follow across the page.
                BackgroundColor = i % 2 == 1 ? ColorPalette.Slate[50] : null,
                RunningValue = item.Amount / 100m,
            });
        }
        int quantity = items.Sum(item => item.Quantity);
        int amount = items.Sum(item => item.Amount);
        table.AddFooter(new TableRow
        {
            Cells =
            [
                new TableCell { Content = new TextBlock("Total") { FontWeight = FontWeight.Bold }, ColumnSpan = 2 },
                new TableCell { Content = new TextBlock(quantity.ToString(CultureInfo.InvariantCulture)) { FontWeight = FontWeight.Bold, Alignment = TextAlignment.Right } },
                new TableCell { Content = new TextBlock(Money(amount)) { FontWeight = FontWeight.Bold, Alignment = TextAlignment.Right } },
            ],
        });
        return table;
    }

    private static string Money(int pence) => (pence / 100m).ToString("F2", CultureInfo.InvariantCulture);
}
