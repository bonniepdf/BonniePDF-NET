using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using static BonniePDF.Examples.Templates.RegionalInvoices.InvoiceKit;

namespace BonniePDF.Examples.Templates.RegionalInvoices;

/// <summary>
/// US sales-tax invoice. The United States has no national VAT/GST and no
/// e-invoicing mandate; sales tax is levied per state (and often per
/// county/city) on the taxable subtotal. This renders a clean commercial
/// invoice in that idiom: a destination-based state + county sales-tax
/// breakdown, a purchase-order reference, USD amounts, and a remittance line.
/// Fictional data throughout.
/// </summary>
[System.ComponentModel.DisplayName("US Sales-Tax Invoice")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("US commercial invoice with destination-based state and county sales tax, PO reference, and USD remittance.")]
public static class UsSalesInvoiceExample
{
    private static readonly Color Accent = ColorPalette.Blue[700];
    private const string Usd = "$";

    private const decimal StateTaxRate = 0.0625m; // CA statewide
    private const decimal CountyTaxRate = 0.0225m; // district add-on

    private static readonly InvoiceItem[] Items =
    [
        new("Managed cloud hosting", 1, 2400m, "Production tier, monthly"),
        new("Platform engineering", 48, 185m, "Senior rate"),
        new("Onboarding & training", 2, 950m, "Two half-day sessions"),
        new("Priority support", 1, 1800m, "Annual retainer (prorated)"),
    ];

    public static Document Generate()
    {
        DateTime now = DateTime.Now;
        int year = now.Year;
        DateTime due = now.AddDays(30);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-US";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);

        doc.Metadata.Title = $"Invoice INV-US-{year}-0148";
        doc.Metadata.Author = "Sample Company Inc.";
        doc.Metadata.Subject = "US commercial invoice with destination-based state and county sales tax.";

        VerticalStack root = new();
        root.AddChild(AccentBar(Accent));

        // ── Header: seller | INVOICE ────────────────────────────────────────
        VerticalStack seller = new();
        seller.AddChild(Cell("Sample Company Inc.", size: 16, bold: true, color: Ink, margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "500 Market Street, Suite 1200", "San Francisco, CA 94105", "EIN 47-0912288");

        VerticalStack titleStack = new();
        titleStack.AddChild(new Heading("INVOICE")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 26,
            TextColor = Accent,
            Alignment = TextAlignment.Right,
        });
        titleStack.AddChild(Cell($"INV-US-{year}-0148", color: Muted, align: TextAlignment.Right, margin: new Thickness(0, 2, 0, 0)));

        Grid header = new(1.3f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 16, 0, 18) };
        header.AddRow(seller, titleStack);
        root.AddChild(header);

        // ── Bill-to + details ────────────────────────────────────────────────
        VerticalStack details = new();
        details.AddChild(TotalLine("Invoice date", now.ToString("MMM d, yyyy")));
        details.AddChild(TotalLine("Due date (Net 30)", due.ToString("MMM d, yyyy")));
        details.AddChild(TotalLine("Purchase order", "PO-88213"));
        details.AddChild(TotalLine("Ship to", "Seattle, WA"));

        Grid meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 18),
        };
        meta.AddRow(
            Labelled("BILL TO", Lines(Muted, "Sample Customer LLC", "Accounts Payable", "18 Pioneer Square", "Seattle, WA 98104"), Accent),
            Labelled("DETAILS", details, Accent));
        root.AddChild(meta);

        // ── Line items ──────────────────────────────────────────────────────
        Table table = new(3f, 0.7f, 1f, 1f)
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
            Cell("Amount", bold: true, align: TextAlignment.Right));

        decimal subtotal = 0m;
        foreach (InvoiceItem it in Items)
        {
            decimal amount = it.Qty * it.UnitPrice;
            subtotal += amount;
            VerticalStack desc = new();
            desc.AddChild(Cell(it.Description));
            if (it.Detail is not null)
            {
                desc.AddChild(Cell(it.Detail, size: 8, color: Muted));
            }

            table.AddRow(
                desc,
                Cell(it.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture), align: TextAlignment.Center),
                Cell(Money(it.UnitPrice, Usd), align: TextAlignment.Right),
                Cell(Money(amount, Usd), align: TextAlignment.Right));
        }

        root.AddChild(table);
        root.AddChild(Divider(Rule, 0.5f));

        // ── Totals: sales tax is state + county on the taxable subtotal ─────
        decimal stateTax = subtotal * StateTaxRate;
        decimal countyTax = subtotal * CountyTaxRate;
        decimal total = subtotal + stateTax + countyTax;

        VerticalStack totals = new();
        totals.AddChild(TotalLine("Subtotal", Money(subtotal, Usd)));
        totals.AddChild(TotalLine("CA state sales tax (6.25%)", Money(stateTax, Usd)));
        totals.AddChild(TotalLine("King County district tax (2.25%)", Money(countyTax, Usd)));
        totals.AddChild(Divider(Ink, 0.6f));
        totals.AddChild(TotalLine("Total due (USD)", Money(total, Usd), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Blue[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 6, 0, 0) };
        totalsRow.AddChild(new Container(), 1);
        totalsRow.AddChild(totalsBox, 1);
        root.AddChild(totalsRow);

        // ── Remittance note ─────────────────────────────────────────────────
        Container note = Labelled(
            "REMITTANCE",
            Lines(
                Muted,
                "Please reference invoice INV-US-0148 and PO-88213 with payment.",
                "ACH: Routing 121000248  ·  Account ****4471  ·  Sample Company Inc.",
                "Checks payable to Sample Company Inc., 500 Market Street, San Francisco, CA 94105."),
            Accent);
        note.Padding = new Thickness(0, 22, 0, 0);
        root.AddChild(note);

        doc.Add(root);
        return doc;
    }
}
