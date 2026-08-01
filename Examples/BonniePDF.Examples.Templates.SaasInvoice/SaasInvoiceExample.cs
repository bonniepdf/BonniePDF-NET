using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.SaasInvoice;

/// <summary>
/// Accessible SaaS invoice: PDF/A-2a + PDF/UA-1 tagged output with a line-items
/// table, totals, and a QR "pay by bank" code (with alt text). Headings carry
/// their semantic role (H1 "Invoice", H2 "How to pay"), every figure has alt
/// text, and the line items are a tagged table - so the document is both
/// archival and screen-reader friendly.
/// </summary>
[System.ComponentModel.DisplayName("Accessible SaaS Invoice")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Accessible (PDF/UA-1) and archival (PDF/A-2a) subscription invoice: tagged line-items table, totals, and a QR \"pay by bank\" code with alt text.")]
public static class SaasInvoiceExample
{
    // ── Palette (Slate neutrals + indigo brand) ───────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color BodyCol = ColorPalette.Slate[600];
    private static readonly Color Mute = ColorPalette.Slate[500];
    private static readonly Color RuleCol = ColorPalette.Slate[200];
    private static readonly Color Brand = ColorPalette.Indigo[600];
    private static readonly Color Panel = ColorPalette.Slate[50];

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
        brand.AddChild(Tb("SAMPLE COMPANY", 15, Brand, bold: true));
        brand.AddChild(Tb("Infrastructure for builders", 9, Mute));

        VerticalStack meta = new();
        meta.AddChild(Tb("Invoice  INV-2026-0042", 10, Ink, bold: true, align: TextAlignment.Right));
        meta.AddChild(Tb("Issued  18 Jun 2026", 9, Mute, align: TextAlignment.Right));
        meta.AddChild(Tb("Due  02 Jul 2026", 9, Mute, align: TextAlignment.Right));

        Grid headerTable = new(1f, 1f) { Border = Border.None, CellPadding = 0 };
        headerTable.AddRow(brand, meta);
        doc.Add(headerTable);

        doc.Add(Rule(14));

        doc.Add(new Heading("Invoice") { Level = 1 });

        // ── Bill-to / from ────────────────────────────────────────────────────
        VerticalStack billTo = new();
        billTo.AddChild(Tb("BILLED TO", 8, Mute, bold: true, bottom: 4));
        billTo.AddChild(Tb("Sample Customer Ltd", 10, Ink, bold: true));
        billTo.AddChild(Tb("Accounts Payable\n19 Enfield Road, London N1 5BA\nbilling@example.com", 9, BodyCol));

        VerticalStack from = new();
        from.AddChild(Tb("FROM", 8, Mute, bold: true, bottom: 4, align: TextAlignment.Right));
        from.AddChild(Tb("Sample Company Ltd", 10, Ink, bold: true, align: TextAlignment.Right));
        from.AddChild(Tb("4 Tenter Ground, London E1 7NH\nVAT GB 482 9173 55", 9, BodyCol, align: TextAlignment.Right));

        Grid addr = new(1f, 1f)
        {
            Border = Border.None,
            CellPadding = 0,
            Margin = new Thickness(0, 8, 0, 16),
        };
        addr.AddRow(billTo, from);
        doc.Add(addr);

        // ── Line items (tagged table) ─────────────────────────────────────────
        Table items = new(3f, 0.8f, 1f, 1f)
        {
            CellPadding = 8,
            Border = Border.None,
            HeaderBackgroundColor = Panel,
            HeaderTextColor = Ink,
        };
        items.AddHeader(
            Cell("Description", 9, Ink, TextAlignment.Left, bold: true),
            Cell("Qty", 9, Ink, TextAlignment.Center, bold: true),
            Cell("Unit", 9, Ink, TextAlignment.Right, bold: true),
            Cell("Amount", 9, Ink, TextAlignment.Right, bold: true));

        (string Desc, int Qty, double Unit)[] lines =
        {
            ("Pro plan - 25 seats (monthly)", 25, 18.0),
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
                Cell(desc, 9, BodyCol, TextAlignment.Left),
                Cell(qty.ToString(CultureInfo.InvariantCulture), 9, BodyCol, TextAlignment.Center),
                Cell(Money(unit), 9, BodyCol, TextAlignment.Right),
                Cell(Money(amount), 9, Ink, TextAlignment.Right));
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
        doc.Add(new Heading("How to pay") { Level = 2 });

        VerticalStack payInfo = new();
        payInfo.AddChild(Tb("Pay by bank transfer or scan to pay", 10, Ink, bold: true, bottom: 6));
        payInfo.AddChild(Tb("Account name  Sample Company Ltd", 9, BodyCol, bottom: 2));
        payInfo.AddChild(Tb("Sort code  04-00-72    Account  19483055", 9, BodyCol, bottom: 2));
        payInfo.AddChild(Tb("Reference  INV-2026-0042", 9, BodyCol, bottom: 2));
        payInfo.AddChild(Tb("Amount  " + Money(total), 9, Ink, bold: true));

        QRCode qr = new()
        {
            Data = "https://pay.example.com/inv/INV-2026-0042",
            Size = 96,
            QuietZone = 1,
            ErrorCorrection = QrErrorCorrection.Medium,
            AltText = "QR code to pay invoice INV-2026-0042 online, total £" + total.ToString("0.00", CultureInfo.InvariantCulture),
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

        doc.Add(Tb("Thank you for your business. Payment is due within 14 days.", 8.5f, Mute));

        return doc;
    }

    private static string Money(double n) => "£" + n.ToString("N2", CultureInfo.InvariantCulture);

    private static void TotalRow(Table t, string label, string value, bool bold = false)
    {
        float size = bold ? 11 : 9;
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
            TextColor = color,
        };
        if (align.HasValue) t.Alignment = align.Value;
        if (bottom > 0) t.Margin = new Thickness(0, 0, 0, bottom);
        return t;
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
        Child = new TextBlock { Text = " ", FontSize = 0.5f },
        Padding = new Thickness(0, 0.4f, 0, 0.4f),
        Margin = new Thickness(0, top, 0, 8),
    };
}
