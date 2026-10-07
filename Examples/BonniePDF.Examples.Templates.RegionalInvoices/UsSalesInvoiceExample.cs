using System.Globalization;
using BonniePDF;

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
[System.ComponentModel.Description(
    "US commercial invoice with destination-based state and county sales tax, PO reference, and USD remittance.")]
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

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
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

        doc.Add(AccentBar(Accent));

        // ── Header: seller | INVOICE ────────────────────────────────────────
        VerticalStack seller = new();
        seller.Add(Cell("Sample Company Inc.", size: 16, bold: true, color: Ink, margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "500 Market Street, Suite 1200", "San Francisco, CA 94105", "EIN 47-0912288");

        VerticalStack titleStack = new();
        titleStack.Add(new Heading("INVOICE")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 26,
            Color = Accent,
            Alignment = TextAlignment.Right,
        });
        titleStack.Add(Cell($"INV-US-{year}-0148", color: Muted, align: TextAlignment.Right,
            margin: new Thickness(0, 2, 0, 0)));

        Grid header = new(1.3f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 16, 0, 18) };
        header.AddRow(seller, titleStack);
        doc.Add(header);

        // ── Bill-to + details ────────────────────────────────────────────────
        VerticalStack details = new();
        details.Add(TotalLine("Invoice date", now.ToString("MMM d, yyyy")));
        details.Add(TotalLine("Due date (Net 30)", due.ToString("MMM d, yyyy")));
        details.Add(TotalLine("Purchase order", "PO-88213"));
        details.Add(TotalLine("Ship to", "Seattle, WA"));

        Grid meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 18),
        };
        meta.AddRow(
            Labelled("BILL TO",
                Lines(Muted, "Sample Customer LLC", "Accounts Payable", "18 Pioneer Square", "Seattle, WA 98104"),
                Accent),
            Labelled("DETAILS", details, Accent));
        doc.Add(meta);

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
            desc.Add(Cell(it.Description));
            if (it.Detail is not null)
            {
                desc.Add(Cell(it.Detail, size: 8, color: Muted));
            }

            table.AddRow(
                desc,
                Cell(it.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture), align: TextAlignment.Center),
                Cell(Money(it.UnitPrice, Usd), align: TextAlignment.Right),
                Cell(Money(amount, Usd), align: TextAlignment.Right));
        }

        doc.Add(table);
        doc.Add(Divider(Rule, 0.5f));

        // ── Totals: sales tax is state + county on the taxable subtotal ─────
        decimal stateTax = subtotal * StateTaxRate;
        decimal countyTax = subtotal * CountyTaxRate;
        decimal total = subtotal + stateTax + countyTax;

        VerticalStack totals = new();
        totals.Add(TotalLine("Subtotal", Money(subtotal, Usd)));
        totals.Add(TotalLine("CA state sales tax (6.25%)", Money(stateTax, Usd)));
        totals.Add(TotalLine("King County district tax (2.25%)", Money(countyTax, Usd)));
        totals.Add(Divider(Ink, 0.6f));
        totals.Add(TotalLine("Total due (USD)", Money(total, Usd), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Blue[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new()
            { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 6, 0, 0) };
        totalsRow.Add(new Container(), 1);
        totalsRow.Add(totalsBox, 1);
        doc.Add(totalsRow);

        // ── Remittance note ─────────────────────────────────────────────────
        TextBlock remittance = Lines(Muted,
            "Please reference invoice INV-US-0148 and PO-88213 with payment.",
            "ACH: Routing 121000248  ·  Account ****4471  ·  Sample Company Inc.",
            "Checks payable to Sample Company Inc., 500 Market Street, San Francisco, CA 94105.");
        remittance.FontSize = 8;
        Container note = Labelled("REMITTANCE", remittance, Accent);
        note.Padding = new Thickness(0, 22, 0, 0);
        doc.Add(note);

        return doc;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    // 600, not 500: reads against tinted panels in the invoices that use it, and
    // 500 falls to 4.35-4.38:1 there against the 4.5:1 WCAG AA threshold.
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color HeaderBg = ColorPalette.Slate[100];

    private static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with invoice-friendly defaults (9pt Helvetica, slate ink).</summary>
    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left, Thickness? margin = null)
    {
        TextBlock b = new()
        {
            Text = text,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color ?? Ink,
            Alignment = align,
        };
        if (margin.HasValue)
        {
            b.Margin = margin.Value;
        }

        return b;
    }

    /// <summary>A monetary string using the region's symbol and culture.</summary>
    private static string Money(decimal value, string symbol, string culture = "en-US") => symbol + value.ToString("N2", CultureInfo.GetCultureInfo(culture));

    /// <summary>A block of plain text lines, e.g. a postal address. A single TextBlock
    /// using hard <c>\n</c> breaks, so the block tags as one paragraph under PDF/UA.</summary>
    private static TextBlock Lines(Color color, params string[] items) => Cell(string.Join('\n', items), color: color);

    /// <summary>Appends plain text lines to an existing stack (e.g. after a bold company name).</summary>
    private static void AddLines(VerticalStack stack, Color color, params string[] items) => stack.Add(Lines(color, items));

    /// <summary>A labelled block: a bold eyebrow label over a body element.</summary>
    private static Container Labelled(string label, IElement body, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label, size: 8f, bold: true, color: accent, margin: new Thickness(0, 0, 0, 5)));
        stack.Add(body);
        return new Container
        {
            Child = stack
        };
    }

    /// <summary>A single "label ....... value" totals line, weighted so the value hugs the right edge.</summary>
    private static HorizontalStack TotalLine(string label, string value, bool bold = false, float size = 9f, Color? color = null)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 3, 0, 3),
        };
        row.Add(Cell(label, size: size, bold: bold, color: color ?? Muted), 3);
        row.Add(Cell(value, size: size, bold: bold, color: color ?? Ink, align: TextAlignment.Right), 2);
        return row;
    }

    /// <summary>A thin horizontal divider used between totals and the grand total.</summary>
    private static Container Divider(Color color, float thickness = 0.6f) => new()
    {
        BackgroundColor = color,
        Height = thickness,
        Margin = new Thickness(0, 5, 0, 5),
    };

    /// <summary>A full-width coloured accent bar - the top rule that anchors each invoice.</summary>
    private static Container AccentBar(Color color, float height = 5f) => new()
    {
        BackgroundColor = color,
        Height = height,
    };

    /// <summary>A single invoice line item shared by the regional invoice showcases.</summary>
    private sealed record InvoiceItem(string Description, int Qty, decimal UnitPrice, string? Detail = null);
}
