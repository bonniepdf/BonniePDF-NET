using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>An 80 mm receipt with long names, discounts, returns, and explicit font coverage.</summary>
[System.ComponentModel.DisplayName("Point-of-Sale Receipt: Long Basket")]
[System.ComponentModel.Category("Retail & Events")]
[System.ComponentModel.Description("An 80 mm receipt with a long basket, accented names, per-line discounts, a return, and scannable receipt codes.")]
public static class LongBasketReceiptExample
{
    public sealed record ReceiptLine(string Name, int Quantity, long UnitPence, long DiscountPence);

    public static Document Generate()
    {
        var lines = Enumerable.Range(0, 35).Select(i => new ReceiptLine(i % 3 == 0 ? $"Café crème, oat milk and freshly baked almond croissant — item {i + 1}" : $"Farmhouse pantry selection {i + 1}", 1, 350 + i * 10, i % 7 == 0 ? 50 : 0)).ToList();
        lines.Add(new ReceiptLine("Returned reusable cup", -1, 500, 0));
        return BuildReceipt(lines, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSans-Regular.ttf")));
    }

    public static long CalculateTotal(IReadOnlyList<ReceiptLine> lines)
    {
        long total = 0;
        foreach (var line in lines)
        {
            if (line.Quantity == 0 || line.UnitPence < 0 || line.DiscountPence < 0 || (line.Quantity < 0 && line.DiscountPence != 0) || (line.Quantity > 0 && line.DiscountPence > checked(line.Quantity * line.UnitPence))) throw new ArgumentException("Invalid receipt amount or return discount.");
            total = checked(total + line.Quantity * line.UnitPence - line.DiscountPence);
        }
        return total;
    }

    public static Document BuildReceipt(IReadOnlyList<ReceiptLine> lines, byte[] font)
    {
        long total = CalculateTotal(lines);
        var doc = new Document(new DocumentSettings {
            PageSize = new PdfSize(226.77f, 720, "Receipt 80mm"), Margin = 16, Language = "en-GB", MissingGlyphs = MissingGlyphs.Throw,
            FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Receipt 4022 · {{CurrentPage}} / {{TotalPages}}") { FontSize = 8, Alignment = TextAlignment.Center }) }) { Metadata = new DocumentMetadata { Title = "Long-basket receipt #4022" } };
        var face = doc.Fonts.Add(font);
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily(face.FontName).WithSize(9);
        doc.Add(new Heading("SAMPLE COFFEE") { Level = 1, FontSize = 17 });
        doc.Add(new TextBlock("Receipt #4022\n25 October 2026 · GBP\nCustomer: Zoë Dvořák") { Margin = new Thickness(0, 0, 0, 12) });
        var table = new Table(4, 0.6f, 1.1f) { RepeatHeadersOnNewPage = true, CellPadding = 4 };
        table.AddHeader("Item", "Qty", "GBP");
        foreach (var line in lines) table.AddRow(new TextBlock(line.Name + (line.DiscountPence != 0 ? $" (discount {Money(line.DiscountPence)} GBP)" : "")),
            new TextBlock(line.Quantity.ToString(CultureInfo.InvariantCulture)) { Alignment = TextAlignment.Right }, new TextBlock(Money(line.Quantity * line.UnitPence - line.DiscountPence)) { Alignment = TextAlignment.Right });
        if (lines.Count == 0) table.AddRow("No purchases", "", "0.00");
        doc.Add(table);
        var summary = new VerticalStack { Spacing = 10, Margin = new Thickness(0, 12, 0, 0) };
        summary.Add(new TextBlock($"TOTAL {Money(total)} GBP") { FontName = face.FontName, FontSize = 12 });
        summary.Add(new TextBlock("Discounts are shown per line. Negative quantities are returns.") { FontName = face.FontName, FontSize = 9 });
        summary.Add(new QRCode { Data = "https://example.com/receipts/4022", Size = 72, AltText = "Open receipt 4022 online." });
        summary.Add(new Barcode { Value = "4022", Symbology = BarcodeSymbology.Code128, ModuleWidth = 1.5f, Height = 38, AltText = "Receipt number 4022." });
        summary.Add(new TextBlock("Thank you for shopping with us.") { FontName = face.FontName, FontSize = 9 });
        doc.Add(new PreventPageBreak { Child = summary });
        return doc;
    }

    private static string Money(long pence) => (pence / 100m).ToString("F2", CultureInfo.InvariantCulture);
}
