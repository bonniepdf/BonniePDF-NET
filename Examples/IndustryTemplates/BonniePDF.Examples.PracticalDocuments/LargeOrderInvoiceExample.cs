using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>A 72-line invoice: a styled header row that repeats on every page, a carried-forward subtotal at each page break, per-line discounts, mixed VAT rates, and the totals kept together on the last page.</summary>
[System.ComponentModel.DisplayName("Business Invoice: Large Order")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("A 72-line, three-page invoice: a styled header row repeated on every page, the net subtotal carried forward at each page break, line discounts and mixed VAT rates, and a VAT summary and total due kept together on the last page.")]
public static class LargeOrderInvoiceExample
{
    public sealed record InvoiceLine(string Description, int Quantity, long UnitPence, long DiscountPence, int TaxPercent);
    public sealed record Totals(long NetPence, long TaxPence, long GrossPence, SortedDictionary<int, (long NetPence, long TaxPence)> Rates);

    /// <summary>Product, unit price in pence, VAT percent - the supplier's catalogue.</summary>
    private static readonly (string Product, long UnitPence, int TaxPercent)[] Catalogue =
    [
        ("LED panel 600 × 600, 40 W, 4000 K", 2_450, 20),
        ("Emergency exit sign, maintained, 3 h", 4_890, 20),
        ("Loft insulation roll, 200 mm × 5.2 m", 2_175, 0),
        ("Thermostatic radiator valve, 15 mm", 1_299, 20),
        ("Smart heating controller, wall-mounted", 13_900, 20),
        ("Draught-proofing strip, 10 m", 699, 0),
        ("Fire door closer, adjustable, size 2–4", 3_650, 20),
        ("Mobility grab rail, 600 mm, white", 2_840, 0),
        ("Hand sanitiser refill, 5 l", 1_575, 20),
        ("Children’s safety gate, pressure-fit", 3_299, 20),
        ("Low-energy extractor fan, 100 mm", 5_820, 5),
        ("Solar PIR floodlight, 20 W", 4_495, 5),
        ("Air-source heat pump service kit", 18_750, 5),
        ("Water-saving tap aerator, pack of 10", 1_890, 20),
        ("Ceiling tile, mineral fibre, box of 16", 3_120, 20),
        ("Fire extinguisher, 2 kg CO₂, annual service", 2_250, 20),
        ("Anti-slip stair nosing, 1 m", 1_640, 20),
        ("Carbon monoxide alarm, sealed battery", 2_199, 20),
        ("Wall-mounted defibrillator cabinet", 26_500, 0),
        ("Cleaning trolley, two-bucket", 9_850, 20),
        ("Signage pack, accessible toilets", 4_120, 0),
        ("Window restrictor, cable lock", 899, 20),
        ("Lighting control module, DALI", 11_400, 20),
        ("Batteries, AA lithium, pack of 24", 2_380, 20),
    ];
    private static readonly string[] Sites = ["Riverside block A", "Riverside block B", "Station Road depot"];
    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");

    public static Document Generate() => BuildInvoice(Enumerable.Range(0, 72).Select(i =>
    {
        var (product, unitPence, taxPercent) = Catalogue[i % Catalogue.Length];
        return new InvoiceLine($"{product}, {Sites[i / Catalogue.Length]}", i % 4 + 1, unitPence, i % 5 == 0 ? 150 : 0, taxPercent);
    }).ToArray(), "PO-WFM-44817");

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
        HorizontalStack footer = new() { WidthDistribution = WidthDistribution.Equal };
        footer.Add(Text("Northfield Supply Ltd  ·  VAT GB 318 4402 17", 8, ColorPalette.Slate[500]));
        footer.Add(Text("INV-2026-072  ·  page {{CurrentPage}} of {{TotalPages}}", 8, ColorPalette.Slate[500], align: TextAlignment.Right));
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Margin = 42,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(ColorPalette.Slate[700]),
            FooterTemplate = PageTemplate.FromElements(@default: footer)
        })
        { Metadata = new DocumentMetadata { Title = "Large-order invoice INV-2026-072" } };

        // ── Header: brand and invoice details ──
        VerticalStack brand = new() { Spacing = 2 };
        brand.Add(new Heading("NORTHFIELD SUPPLY") { Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 20, LetterSpacing = 0.5f, Color = ColorPalette.Violet[800] });
        brand.Add(Text("Facilities and maintenance supplies", 9, ColorPalette.Slate[500]));
        VerticalStack meta = new() { Spacing = 2 };
        meta.Add(Text("INVOICE", 9, ColorPalette.Slate[900], bold: true, align: TextAlignment.Right));
        meta.Add(Text("INV-2026-072", 14, ColorPalette.Slate[900], bold: true, align: TextAlignment.Right));
        meta.Add(Text("Issued 25 October 2026  ·  due 24 November 2026", 9, ColorPalette.Slate[500], align: TextAlignment.Right));
        Grid header = new(1f, 1f) { Border = new Border(0), CellPadding = 0, Margin = new Thickness(0, 0, 0, 14) };
        header.AddRow(brand, meta);
        doc.Add(header);

        Grid parties = new(1f, 1f, 1f) { Border = new Border(0), CellPadding = 0, Margin = new Thickness(0, 0, 0, 16) };
        parties.AddRow(
            Party("BILL TO", "Westborough Facilities and Maintenance Services Ltd", "Accounts payable\n4 Minster Court, York YO1 7HH"),
            Party("DELIVER TO", "Riverside Industrial Estate", "Blocks A and B, and Station Road depot\nYork YO1 1AA"),
            Party("REFERENCES", string.IsNullOrEmpty(purchaseOrder) ? "No purchase order" : $"Purchase order {purchaseOrder}", "Account WFM-0193\nCurrency GBP"));
        doc.Add(parties);

        // ── Line items: the header row repeats at the top of every page, and each page
        // break carries the net subtotal forward to the next ──
        Table table = new(4.4f, 0.6f, 1f, 1.1f, 0.6f, 1.1f)
        {
            RepeatHeadersOnNewPage = true,
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            HeaderBackgroundColor = ColorPalette.Slate[800],
            HeaderTextColor = Color.White,
            FooterBackgroundColor = ColorPalette.Slate[100],
            FooterTextColor = ColorPalette.Slate[900],
            RunningTotalColumn = 5
        };
        table.AddHeader(
            Text("Description", 8.5f, Color.White, bold: true),
            Text("Qty", 8.5f, Color.White, bold: true, align: TextAlignment.Right),
            Text("Unit £", 8.5f, Color.White, bold: true, align: TextAlignment.Right),
            Text("Discount £", 8.5f, Color.White, bold: true, align: TextAlignment.Right),
            Text("VAT", 8.5f, Color.White, bold: true, align: TextAlignment.Right),
            Text("Net £", 8.5f, Color.White, bold: true, align: TextAlignment.Right));
        foreach (var line in lines)
        {
            Color ink = ColorPalette.Slate[800];
            long net = line.Quantity * line.UnitPence - line.DiscountPence;
            IElement[] cells =
            [
                Text(line.Description, 9, ink),
                Text(line.Quantity.ToString(CultureInfo.InvariantCulture), 9, ink, align: TextAlignment.Right),
                Text(Money(line.UnitPence), 9, ink, align: TextAlignment.Right),
                Text(line.DiscountPence != 0 ? Money(line.DiscountPence) : "\u2013", 9, ink, align: TextAlignment.Right),
                Text($"{line.TaxPercent}%", 9, ink, align: TextAlignment.Right),
                Text(Money(net), 9, ink, align: TextAlignment.Right),
            ];
            table.AddRow(new TableRow { Cells = cells.Select(content => new TableCell { Content = content }).ToArray(), RunningValue = net / 100m });
        }
        if (lines.Count == 0) table.AddRow("No billable items", "", "", "", "", "0.00");
        doc.Add(table);

        // ── Totals: kept together, so they always appear whole on the last page ──
        Table vat = new(1f, 1.4f, 1.4f) { CellPadding = 5, Border = new Border(0), HeaderBackgroundColor = ColorPalette.Slate[100], HeaderTextColor = ColorPalette.Slate[900] };
        vat.AddHeader(
            Text("VAT rate", 8.5f, ColorPalette.Slate[900], bold: true),
            Text("Net £", 8.5f, ColorPalette.Slate[900], bold: true, align: TextAlignment.Right),
            Text("VAT £", 8.5f, ColorPalette.Slate[900], bold: true, align: TextAlignment.Right));
        foreach (var (rate, group) in totals.Rates.Reverse())
            vat.AddRow(
                Text($"{rate}%", 9, ColorPalette.Slate[700]),
                Text(Money(group.NetPence), 9, ColorPalette.Slate[700], align: TextAlignment.Right),
                Text(Money(group.TaxPence), 9, ColorPalette.Slate[700], align: TextAlignment.Right));

        Grid sums = new(1f, 1f) { Border = new Border(0), CellPadding = 4 };
        sums.AddRow(Text("Net", 10, ColorPalette.Slate[700], align: TextAlignment.Right), Text($"£{Money(totals.NetPence)}", 10, ColorPalette.Slate[700], align: TextAlignment.Right));
        sums.AddRow(Text("VAT", 10, ColorPalette.Slate[700], align: TextAlignment.Right), Text($"£{Money(totals.TaxPence)}", 10, ColorPalette.Slate[700], align: TextAlignment.Right));

        HorizontalStack dueLine = new() { WidthDistribution = WidthDistribution.Equal };
        dueLine.Add(Text("Total due", 13, ColorPalette.Violet[900], bold: true));
        dueLine.Add(Text($"£{Money(totals.GrossPence)}", 13, ColorPalette.Violet[900], bold: true, align: TextAlignment.Right));
        Container due = new() { BackgroundColor = ColorPalette.Violet[50], CornerRadius = 6, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 6, 0, 0), Child = dueLine };

        VerticalStack vatColumn = new() { Spacing = 6 };
        vatColumn.Add(new Heading("VAT summary") { Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = ColorPalette.Slate[900] });
        vatColumn.Add(vat);
        VerticalStack totalsColumn = new() { Spacing = 6 };
        totalsColumn.Add(new Heading("Invoice totals") { Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = ColorPalette.Slate[900] });
        totalsColumn.Add(sums);
        totalsColumn.Add(due);

        HorizontalStack totalsRow = new() { Spacing = 24, WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 16, 0, 0) };
        totalsRow.Add(vatColumn, 3);
        totalsRow.Add(totalsColumn, 2);

        VerticalStack closing = new();
        closing.Add(totalsRow);
        closing.Add(Text("Payment terms: 30 days by bank transfer to Northfield Supply Ltd, sort code 20-45-77, account 61839204. Quote INV-2026-072 with your payment.", 9, ColorPalette.Slate[600], top: 14));
        doc.Add(new PreventPageBreak { Child = closing });
        return doc;
    }

    private static string Money(long pence) => (pence / 100m).ToString("N2", EnGb);

    private static TextBlock Text(string content, float size, Color color, bool bold = false, TextAlignment align = TextAlignment.Left, float top = 0)
    {
        TextBlock block = new(content)
        {
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
            Alignment = align
        };
        if (top > 0) block.Margin = new Thickness(0, top, 0, 0);
        return block;
    }

    /// <summary>A labelled address block.</summary>
    private static VerticalStack Party(string label, string name, string detail)
    {
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(Text(label, 7.5f, ColorPalette.Slate[500], bold: true));
        stack.Add(Text(name, 9.5f, ColorPalette.Slate[900], bold: true));
        stack.Add(Text(detail, 9, ColorPalette.Slate[600]));
        return stack;
    }
}
