using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Invoices.SaasInvoice;

/// <summary>
/// Accessible SaaS invoice: PDF/A-2a + PDF/UA-1 tagged output with a line-items
/// table, totals, and a QR "pay by bank" code (with alt text). Headings carry
/// their semantic role (H1 "Invoice", H2 "How to pay"), every figure has alt
/// text, and the line items are a tagged table - so the document is both
/// archival and screen-reader friendly.
/// </summary>
[System.ComponentModel.DisplayName("Accessible SaaS Invoice")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description(
    "An accessible subscription invoice for a Pro plan: billing period and status, per-seat and storage line items, a proration line, VAT, a prominent total and a QR payment code.")]
public static class SaasInvoiceExample
{
    // ── Palette (Slate neutrals + violet brand) ───────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color BodyCol = ColorPalette.Slate[700];
    private static readonly Color Mute = ColorPalette.Slate[600];
    private static readonly Color RuleCol = ColorPalette.Slate[200];
    private static readonly Color Brand = ColorPalette.Violet[600];
    private static readonly Color Panel = ColorPalette.Slate[50];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 48,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(BodyCol),
        });

        doc.Metadata.Title = "Invoice INV-2026-0042";
        doc.Metadata.Author = "Sample Company Ltd";
        doc.Metadata.Subject = "Subscription invoice - accessible (PDF/UA) and archival (PDF/A-2a)";

        // ── Header: brand + invoice meta ──────────────────────────────────────
        VerticalStack brand = new();
        brand.Add(Tb("SAMPLE COMPANY", 15, Brand, bold: true));
        brand.Add(Tb("Infrastructure for builders", 9, Mute));

        VerticalStack meta = new();
        meta.Add(Tb("Invoice  INV-2026-0042", 10, Ink, bold: true, align: TextAlignment.Right));
        meta.Add(Tb("Issued  18 Jun 2026", 9, Mute, align: TextAlignment.Right));
        meta.Add(Tb("Due  02 Jul 2026", 9, Mute, align: TextAlignment.Right));

        Grid headerTable = new(1f, 1f) { Border = Border.None, CellPadding = 0 };
        headerTable.AddRow(brand, meta);
        doc.Add(headerTable);

        doc.Add(Rule(14));

        // ── Title with a payment-status pill ──────────────────────────────────
        HorizontalStack titleRow = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 0, 0, 4) };
        titleRow.Add(new Heading("Invoice") { FontName = StandardFonts.HelveticaBold, FontSize = 26, Color = Ink }, 4);
        titleRow.Add(new Container
        {
            BackgroundColor = ColorPalette.Amber[100],
            CornerRadius = 10,
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 6, 0, 0),
            Child = Tb("PAYMENT DUE", 8, ColorPalette.Amber[900], bold: true, align: TextAlignment.Center),
        }, 1);
        doc.Add(titleRow);

        // ── Bill-to / from ────────────────────────────────────────────────────
        VerticalStack billTo = new();
        billTo.Add(Tb("BILLED TO", 8, Mute, bold: true, bottom: 4));
        billTo.Add(Tb("Sample Customer Ltd", 10, Ink, bold: true));
        billTo.Add(Tb("Accounts Payable\n19 Enfield Road, London N1 5BA\nbilling@example.com", 9, BodyCol));

        VerticalStack from = new();
        from.Add(Tb("FROM", 8, Mute, bold: true, bottom: 4, align: TextAlignment.Right));
        from.Add(Tb("Sample Company Ltd", 10, Ink, bold: true, align: TextAlignment.Right));
        from.Add(Tb("4 Tenter Ground, London E1 7NH\nVAT GB 482 9173 55", 9, BodyCol, align: TextAlignment.Right));

        Grid addr = new(1f, 1f)
        {
            Border = Border.None,
            CellPadding = 0,
            Margin = new Thickness(0, 8, 0, 16),
        };
        addr.AddRow(billTo, from);
        doc.Add(addr);

        // ── Subscription summary strip ────────────────────────────────────────
        Grid summary = new(1f, 1.4f, 1f) { Border = Border.None, CellPadding = 0 };
        summary.AddRow(
            Fact("PLAN", "Pro, billed monthly"),
            Fact("BILLING PERIOD", "18 Jun \u2013 17 Jul 2026"),
            Fact("CUSTOMER ID", "CUS-20418"));
        doc.Add(new Container
        {
            BackgroundColor = Panel,
            CornerRadius = 6,
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 16),
            Child = summary,
        });

        // ── Line items (tagged table) ─────────────────────────────────────────
        Table items = new(3f, 0.8f, 1f, 1f)
        {
            CellPadding = 8,
            Border = Border.None,
            HeaderBackgroundColor = Panel,
            HeaderTextColor = Ink,
        };
        items.AddHeader(
            Cell("Description", 10, Ink, TextAlignment.Left, bold: true),
            Cell("Qty", 10, Ink, TextAlignment.Center, bold: true),
            Cell("Unit", 10, Ink, TextAlignment.Right, bold: true),
            Cell("Amount", 10, Ink, TextAlignment.Right, bold: true));

        (string Desc, int Qty, double Unit)[] lines =
        {
            ("Pro plan - 25 seats (monthly)", 25, 18.0),
            ("Proration - 5 seats added 4 Jun (14 of 30 days)", 5, 8.4),
            ("Additional storage - 2 TB", 2, 40.0),
            ("Premium support add-on", 1, 199.0),
            ("Egress overage - June", 1, 36.5),
        };

        double subtotal = 0;
        foreach ((string desc, int qty, double unit) in lines)
        {
            double amount = qty * unit;
            subtotal += amount;
            items.AddRow(
                Cell(desc, 10, BodyCol, TextAlignment.Left),
                Cell(qty.ToString(CultureInfo.InvariantCulture), 10, BodyCol, TextAlignment.Center),
                Cell(Money(unit), 10, BodyCol, TextAlignment.Right),
                Cell(Money(amount), 10, Ink, TextAlignment.Right));
        }

        doc.Add(items);
        doc.Add(Rule(0));

        // ── Totals ────────────────────────────────────────────────────────────
        double vat = subtotal * 0.2;
        double total = subtotal + vat;
        Grid totals = new(2.4f, 1f)
        {
            Border = Border.None,
            CellPadding = 5,
            Margin = new Thickness(0, 4, 0, 0),
        };
        TotalRow(totals, "Subtotal", Money(subtotal));
        TotalRow(totals, "VAT (20%)", Money(vat));
        TotalRow(totals, "Total due", Money(total), bold: true);
        doc.Add(totals);

        // ── Payment panel with QR ─────────────────────────────────────────────
        doc.Add(new Heading("How to pay")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = Ink,
            Margin = new Thickness(0, 14, 0, 2),
        });

        VerticalStack payInfo = new();
        payInfo.Add(Tb("Pay by bank transfer or scan to pay", 10, Ink, bold: true, bottom: 6));
        payInfo.Add(Tb("Account name  Sample Company Ltd", 10, BodyCol, bottom: 2));
        payInfo.Add(Tb("Sort code  04-00-72    Account  19483055", 10, BodyCol, bottom: 2));
        payInfo.Add(Tb("Reference  INV-2026-0042", 10, BodyCol, bottom: 2));
        payInfo.Add(Tb("Amount  " + Money(total), 10, Ink, bold: true));

        QRCode qr = new()
        {
            Data = "https://pay.example.com/inv/INV-2026-0042",
            Size = 96,
            QuietZone = 1,
            ErrorCorrection = QrErrorCorrection.Medium,
            AltText = "QR code to pay invoice INV-2026-0042 online, total £" +
                      total.ToString("0.00", CultureInfo.InvariantCulture),
        };
        Container qrWrap = new() { Child = qr, Padding = new Thickness(6) };

        Grid payTable = new(2.2f, 1f) { Border = Border.None, CellPadding = 0 };
        payTable.AddRow(payInfo, qrWrap);

        doc.Add(new Container
        {
            BackgroundColor = Panel,
            CornerRadius = 8,
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 8, 0, 0),
            Child = payTable,
        });

        doc.Add(new TextBlock("Thank you for your business. Payment is due within 14 days.")
        {
            FontSize = 9,
            Color = Mute,
            Margin = new Thickness(0, 10, 0, 0),
        });

        return doc;
    }

    private static string Money(double n) => "£" + n.ToString("N2", CultureInfo.InvariantCulture);

    private static void TotalRow(Table t, string label, string value, bool bold = false)
    {
        float size = bold ? 15 : 10;
        Color color = bold ? Ink : BodyCol;
        t.AddRow(
            Cell(label, size, color, TextAlignment.Right, bold),
            Cell(value, size, color, TextAlignment.Right, bold));
    }

    private static TextBlock Tb(string content, float size, Color color, bool bold = false,
        TextAlignment? align = null, float bottom = 0)
    {
        TextBlock t = new()
        {
            Text = content,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) t.Alignment = align.Value;
        if (bottom > 0) t.Margin = new Thickness(0, 0, 0, bottom);
        return t;
    }

    /// <summary>A small caps label over its value, for the subscription strip.</summary>
    private static VerticalStack Fact(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 3 };
        stack.Add(Tb(label, 8, Mute, bold: true));
        stack.Add(Tb(value, 10, Ink, bold: true));
        return stack;
    }

    private static TextBlock Cell(string content, float size, Color color, TextAlignment align, bool bold = false)
    {
        TextBlock t = Tb(content, size, color, bold);
        t.Alignment = align;
        return t;
    }

    private static Container Rule(float top) => new()
    {
        BackgroundColor = RuleCol,
        Height = 0.8f,
        Margin = new Thickness(0, top, 0, 8),
    };
}
