using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>A large order with line discounts, mixed rates, and reconciled minor-unit totals.</summary>
public static class LargeOrderInvoiceExample
{
    private static readonly int[] TaxRates = [20, 5, 0];
    public sealed record InvoiceLine(string Description, int Quantity, long UnitPence, long DiscountPence, int TaxPercent);
    public sealed record Totals(long NetPence, long TaxPence, long GrossPence, SortedDictionary<int, (long NetPence, long TaxPence)> Rates);

    public static Document Generate() => BuildInvoice(Enumerable.Range(0, 72).Select(i => new InvoiceLine(
        $"Component {i + 1}: replacement assembly with installation, commissioning and handover notes{(i % 9 == 0 ? "; includes extended warranty and a detailed maintenance schedule for the customer site" : "")}.",
        i % 4 + 1, 1299 + i * 25, i % 5 == 0 ? 150 : 0, TaxRates[i % 3])).ToArray());

    // Integer minor units; tax rounds half up on each line before aggregation.
    public static Totals CalculateTotals(IReadOnlyList<InvoiceLine> lines)
    {
        long netPence = 0, taxPence = 0;
        var rates = new SortedDictionary<int, (long NetPence, long TaxPence)>();
        foreach (var line in lines)
        {
            if (line.Quantity < 1 || line.UnitPence < 0 || line.DiscountPence < 0 || line.TaxPercent is < 0 or > 100) throw new ArgumentException("Invalid invoice line.");
            long net = checked(line.Quantity * line.UnitPence - line.DiscountPence);
            if (net < 0) throw new ArgumentException("Invalid discounted line amount.");
            long tax = checked(net * line.TaxPercent + 50) / 100;
            rates.TryGetValue(line.TaxPercent, out var group);
            rates[line.TaxPercent] = (checked(group.NetPence + net), checked(group.TaxPence + tax));
            netPence = checked(netPence + net); taxPence = checked(taxPence + tax);
        }
        return new Totals(netPence, taxPence, checked(netPence + taxPence), rates);
    }

    public static Document BuildInvoice(IReadOnlyList<InvoiceLine> lines, string? purchaseOrder = null)
    {
        var totals = CalculateTotals(lines);
        var doc = new Document(new DocumentSettings {
            Language = "en-GB", Margin = 42, Conformance = PdfAConformance.PdfA2a, Accessibility = AccessibilityMode.PdfUa1,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[800]),
            FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("INV-2026-072 · Page {{CurrentPage}} of {{TotalPages}}") { FontSize = 9, Alignment = TextAlignment.Right }) }) { Metadata = new DocumentMetadata { Title = "Large-order invoice INV-2026-072" } };
        doc.Add(new Heading("NORTHFIELD SUPPLY") { Level = 1, FontSize = 25, Color = ColorPalette.Indigo[700] });
        doc.Add(new TextBlock("Invoice INV-2026-072 · 25 October 2026 · GBP") { Margin = new Thickness(0, 0, 0, 16) });
        doc.Add(new TextBlock("Bill to: Westborough Facilities and Maintenance Services Limited\nCustomer site: 18 Riverside Industrial Estate, York YO1 1AA") { Margin = new Thickness(0, 0, 0, 12) });
        if (!string.IsNullOrEmpty(purchaseOrder)) doc.Add(new TextBlock($"Purchase order: {purchaseOrder}"));
        var table = new Table(5, 0.5f, 1, 1, 0.6f, 1) { RepeatHeadersOnNewPage = true, CellPadding = 6 };
        table.AddHeader("Description", "Qty", "Unit GBP", "Discount", "Tax %", "Net GBP");
        foreach (var line in lines) table.AddRow(new TextBlock(line.Description),
            Number(line.Quantity.ToString(CultureInfo.InvariantCulture)), Number(Money(line.UnitPence)), Number(Money(line.DiscountPence)), Number(line.TaxPercent.ToString(CultureInfo.InvariantCulture)), Number(Money(line.Quantity * line.UnitPence - line.DiscountPence)));
        if (lines.Count == 0) table.AddRow("No billable items", "", "", "", "", "0.00");
        doc.Add(table);
        var summary = new VerticalStack { Spacing = 7, Margin = new Thickness(0, 16, 0, 0) };
        summary.Add(new Heading("Invoice totals") { Level = 2, FontSize = 17 });
        foreach (var (rate, group) in totals.Rates) summary.Add(new TextBlock($"Tax {rate}%: net {Money(group.NetPence)} · tax {Money(group.TaxPence)} GBP"));
        summary.Add(new TextBlock($"Net {Money(totals.NetPence)} · Tax {Money(totals.TaxPence)} · Total due {Money(totals.GrossPence)} GBP") { FontWeight = FontWeight.Bold });
        summary.Add(new TextBlock("Payment terms: 30 days. Reference INV-2026-072 with your payment."));
        doc.Add(new PreventPageBreak { Child = summary });
        return doc;
    }

    private static TextBlock Number(string value) => new(value) { Alignment = TextAlignment.Right };
    private static string Money(long pence) => (pence / 100m).ToString("F2", CultureInfo.InvariantCulture);
}
