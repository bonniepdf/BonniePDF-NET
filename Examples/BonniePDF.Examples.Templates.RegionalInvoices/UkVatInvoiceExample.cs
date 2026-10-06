using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Templates.RegionalInvoices.InvoiceKit;

namespace BonniePDF.Examples.Templates.RegionalInvoices;

/// <summary>
/// A HMRC-style UK VAT invoice. Post-Brexit the UK runs its own VAT regime, and
/// a valid VAT invoice must show the supplier's VAT registration number, a tax
/// point (supply date), and - crucially - a VAT breakdown by rate. This mixes
/// standard-rated (20%), reduced-rated (5%), and zero-rated (0%) lines and
/// summarises the net / VAT / gross per rate, the way UK accounting software does.
/// GBP throughout; fictional company data.
/// </summary>
[System.ComponentModel.DisplayName("UK VAT Invoice")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("HMRC-style UK VAT invoice with VAT registration number, tax point, and a per-rate VAT breakdown (standard, reduced, zero).")]
public static class UkVatInvoiceExample
{
    private static readonly Color Accent = ColorPalette.Rose[700];
    private const string Gbp = "£";

    private sealed record UkLine(string Description, int Qty, decimal UnitPrice, decimal VatRate, string RateLabel);

    private static readonly UkLine[] Items =
    [
        new("Consultancy services (May 2025)", 12, 480m, 0.20m, "S"),
        new("On-site installation", 1, 1250m, 0.20m, "S"),
        new("Children's safety equipment", 40, 18.5m, 0.05m, "R"),
        new("Printed training manuals", 200, 3.25m, 0m, "Z"),
    ];

    public static Document Generate()
    {
        DateTime now = DateTime.Now;
        int year = now.Year;
        string date = now.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);

        doc.Metadata.Title = $"VAT Invoice INV-UK-{year}-0311";
        doc.Metadata.Author = "Sample Company Ltd";
        doc.Metadata.Subject = "UK VAT invoice with a per-rate VAT breakdown (standard, reduced, zero).";

        doc.Add(AccentBar(Accent));

        // ── Header ────────────────────────────────────────────────────────────
        VerticalStack seller = new();
        seller.Add(Cell("Sample Company Ltd", size: 16, bold: true, color: Ink, margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "Unit 7, Kingswood Business Park", "Bristol BS15 8NY", "United Kingdom");

        seller.Add(Cell("VAT Reg. No. GB 218 4471 09", bold: true, color: Accent, margin: new Thickness(0, 6, 0, 0)));
        seller.Add(Cell("Company No. 09182773 (England & Wales)", size: 8, color: Muted));

        VerticalStack titleStack = new();
        titleStack.Add(new Heading("VAT INVOICE")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = Accent,
            Alignment = TextAlignment.Right,
        });
        titleStack.Add(Cell($"INV-UK-{year}-0311", color: Muted, align: TextAlignment.Right, margin: new Thickness(0, 2, 0, 0)));

        Grid header = new(1.3f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 16, 0, 18) };
        header.AddRow(seller, titleStack);
        doc.Add(header);

        // ── Customer + tax point ────────────────────────────────────────────
        VerticalStack details = new();
        details.Add(TotalLine("Invoice date", date));
        details.Add(TotalLine("Tax point", date));
        details.Add(TotalLine("Customer VAT no.", "GB 442 8810 21"));
        details.Add(TotalLine("Payment terms", "30 days"));

        Grid meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 18),
        };
        meta.AddRow(
            Labelled("INVOICE TO", Lines(Muted, "Sample Customer PLC", "Finance Department", "4 Waterfront Square", "Cardiff CF10 4PA"), Accent),
            Labelled("DETAILS", details, Accent));
        doc.Add(meta);

        // ── Line items (with VAT-rate column) ───────────────────────────────
        Table table = new(3f, 0.6f, 1f, 0.7f, 1f)
        {
            CellPadding = 8,
            Border = Border.None,
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
        };
        table.AddHeader(
            Cell("Description", bold: true),
            Cell("Qty", bold: true, align: TextAlignment.Center),
            Cell("Unit price", bold: true, align: TextAlignment.Right),
            Cell("VAT", bold: true, align: TextAlignment.Center),
            Cell("Net amount", bold: true, align: TextAlignment.Right));

        // Accumulate net per rate for the summary block.
        Dictionary<decimal, decimal> netByRate = new();
        decimal netTotal = 0m;
        foreach (UkLine it in Items)
        {
            decimal net = it.Qty * it.UnitPrice;
            netTotal += net;
            netByRate[it.VatRate] = netByRate.GetValueOrDefault(it.VatRate) + net;
            table.AddRow(
                Cell(it.Description),
                Cell(it.Qty.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Center),
                Cell(Money(it.UnitPrice, Gbp, "en-GB"), align: TextAlignment.Right),
                Cell(it.RateLabel, align: TextAlignment.Center, color: Muted),
                Cell(Money(net, Gbp, "en-GB"), align: TextAlignment.Right));
        }

        doc.Add(table);
        doc.Add(Divider(Rule, 0.5f));

        // ── VAT summary by rate (the defining feature of a UK VAT invoice) ──
        Table summary = new(1.4f, 1f, 1f, 1f)
        {
            CellPadding = 7,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 10, 0, 0),
        };
        summary.AddHeader(
            Cell("VAT rate", bold: true),
            Cell("Net", bold: true, align: TextAlignment.Right),
            Cell("VAT", bold: true, align: TextAlignment.Right),
            Cell("Gross", bold: true, align: TextAlignment.Right));

        decimal vatTotal = 0m;
        foreach (KeyValuePair<decimal, decimal> pair in netByRate.OrderByDescending(p => p.Key))
        {
            decimal vat = pair.Value * pair.Key;
            vatTotal += vat;
            summary.AddRow(
                Cell(RateName(pair.Key)),
                Cell(Money(pair.Value, Gbp, "en-GB"), align: TextAlignment.Right),
                Cell(Money(vat, Gbp, "en-GB"), align: TextAlignment.Right),
                Cell(Money(pair.Value + vat, Gbp, "en-GB"), align: TextAlignment.Right));
        }

        doc.Add(summary);

        // ── Grand total ──────────────────────────────────────────────────────
        VerticalStack totals = new();
        totals.Add(TotalLine("Total net", Money(netTotal, Gbp, "en-GB")));
        totals.Add(TotalLine("Total VAT", Money(vatTotal, Gbp, "en-GB")));
        totals.Add(Divider(Ink, 0.6f));
        totals.Add(TotalLine("Total due (GBP)", Money(netTotal + vatTotal, Gbp, "en-GB"), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Rose[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 10, 0, 0) };
        totalsRow.Add(new Container(), 1);
        totalsRow.Add(totalsBox, 1);
        doc.Add(totalsRow);

        doc.Add(Cell(
            "S = Standard 20%   ·   R = Reduced 5%   ·   Z = Zero-rated 0%",
            size: 8,
            color: Muted,
            margin: new Thickness(0, 20, 0, 0)));

        return doc;
    }

    private static string RateName(decimal rate)
    {
        if (rate == 0.20m)
        {
            return "Standard (20%)";
        }

        if (rate == 0.05m)
        {
            return "Reduced (5%)";
        }

        if (rate == 0m)
        {
            return "Zero-rated (0%)";
        }

        return $"{rate * 100m:0}%";
    }
}
