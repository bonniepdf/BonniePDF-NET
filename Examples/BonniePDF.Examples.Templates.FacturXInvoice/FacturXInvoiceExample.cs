using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.EInvoice;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.FacturXInvoice;

/// <summary>
/// The single EU e-invoice showcase: the polished human-readable invoice layout
/// <em>and</em> the real machine-readable half. <see cref="FacturX.Apply"/> binds
/// the caller-supplied EN 16931 CII XML - it forces PDF/A-3 and embeds
/// <c>factur-x.xml</c> inside the invoice.
///
/// <para>The visible totals mirror the embedded XML exactly (net 4000.00, VAT
/// 760.00, gross 4760.00) - a real e-invoice's human- and machine-readable views
/// must agree. The XML is a caller input; BonniePDF does not generate it.</para>
///
/// <para>Shares the regional-invoice visual grammar (the kit helpers below mirror
/// InvoiceKit). Validated in CI by veraPDF (PDF/A-3b) and the Mustang project
/// (ZUGFeRD / EN 16931). Parity peer of the TypeScript factur-x-invoice.</para>
/// </summary>
[System.ComponentModel.DisplayName("EU E-Invoice (Factur-X / ZUGFeRD, EN 16931)")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A cross-border EU e-invoice following EN 16931 / Factur-X: a polished PDF/A-3 invoice carrying the machine-readable CII invoice XML inside it.")]
public static class FacturXInvoiceExample
{
    private static readonly Color Ink = ColorPalette.Slate[700];
    // 600, not 500: reads against tinted panels elsewhere in the document, and
    // 500 falls to 4.35-4.38:1 there against the 4.5:1 WCAG AA threshold.
    private static readonly Color Muted = ColorPalette.Slate[600];
    private static readonly Color HeaderBg = ColorPalette.Slate[100];
    private static readonly Color RuleCol = ColorPalette.Slate[200];
    private static readonly Color Accent = ColorPalette.Blue[800]; // EU-flag blue

    private const string Eur = "€";
    private const string Locale = "en-GB";
    private const decimal VatRate = 0.19m;

    // Line items reconcile with the embedded XML: 40 × 85 + 1 × 600 = 4000 net.
    private static readonly (string Description, string Detail, int Qty, decimal UnitPrice)[] Items =
    [
        ("Consulting services", "Professional services (40 h)", 40, 85m),
        ("Software license (annual)", "12-month term", 1, 600m),
    ];

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 44,
            // FacturX.Apply forces PDF/A-3b; set here too so the intent is explicit.
            Conformance = PdfAConformance.PdfA3b,
            Language = "en",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink),
        });

        doc.Metadata.Title = "E-Invoice INV-2026-1001";
        doc.Metadata.Author = "Sample Company GmbH";
        doc.Metadata.Subject =
            "EU e-invoice (Factur-X / ZUGFeRD, EN 16931): a PDF/A-3 invoice with the machine-readable CII XML embedded inside it.";

        doc.Add(AccentBar(Accent));

        // ── Header ────────────────────────────────────────────────────────────
        VerticalStack seller = new();
        seller.Add(Cell("Sample Company GmbH", size: 16, bold: true, color: Ink, margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "Musterstrasse 1", "10115 Berlin", "Germany");
        seller.Add(Cell("USt-IdNr. DE 123 456 789", bold: true, color: Accent, margin: new Thickness(0, 6, 0, 0)));

        VerticalStack titleStack = new();
        titleStack.Add(Cell("E-INVOICE", size: 24, bold: true, color: Accent, align: TextAlignment.Right));
        titleStack.Add(Cell("EN 16931 · Factur-X / ZUGFeRD", size: 8, color: Muted, align: TextAlignment.Right, margin: new Thickness(0, 2, 0, 0)));
        titleStack.Add(Cell("INV-2026-1001", color: Muted, align: TextAlignment.Right));

        Grid header = new(1.3f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 16, 0, 18) };
        header.AddRow(seller, titleStack);
        doc.Add(header);

        // ── Buyer + details ─────────────────────────────────────────────────
        VerticalStack details = new();
        details.Add(TotalLine("Invoice date", "07 Jul 2026"));
        details.Add(TotalLine("Due date", "21 Jul 2026"));
        details.Add(TotalLine("Buyer reference", "PO-2026-042"));
        details.Add(TotalLine("Terms", "14 days net"));

        Grid meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(RuleCol),
            Margin = new Thickness(0, 0, 0, 18),
        };
        meta.AddRow(
            Labelled("BILLED TO", Lines(Muted, "Sample Customer", "1 Example Way", "EC1A 1BB London", "United Kingdom"), Accent),
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
            Cell("Net amount", bold: true, align: TextAlignment.Right));

        decimal net = 0m;
        foreach ((string description, string detail, int qty, decimal unitPrice) in Items)
        {
            decimal amount = qty * unitPrice;
            net += amount;
            VerticalStack desc = new();
            desc.Add(Cell(description));
            desc.Add(Cell(detail, size: 8, color: Muted));

            table.AddRow(
                desc,
                Cell(qty.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Center),
                Cell(Money(unitPrice, Eur, Locale), align: TextAlignment.Right),
                Cell(Money(amount, Eur, Locale), align: TextAlignment.Right));
        }

        doc.Add(table);
        doc.Add(Divider(RuleCol, 0.5f));

        // ── Totals (mirror the embedded XML exactly: 4000 / 760 / 4760) ───────
        decimal vat = net * VatRate;
        VerticalStack totals = new();
        totals.Add(TotalLine("Total net", Money(net, Eur, Locale)));
        totals.Add(TotalLine("VAT (19%)", Money(vat, Eur, Locale)));
        totals.Add(Divider(Ink, 0.6f));
        totals.Add(TotalLine("Total due (EUR)", Money(net + vat, Eur, Locale), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Blue[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 6, 0, 0) };
        totalsRow.Add(new Container(), 1);
        totalsRow.Add(totalsBox, 1);
        doc.Add(totalsRow);

        // ── Remittance + embedded-XML panel ─────────────────────────────────
        VerticalStack xmlStack = new();
        xmlStack.Add(Cell("STRUCTURED DATA", bold: true, size: 8, color: Muted, margin: new Thickness(0, 0, 0, 4)));
        xmlStack.Add(Cell("factur-x.xml (EN 16931 CII) is embedded in this", size: 8, color: Muted));
        xmlStack.Add(Cell("PDF/A-3 invoice.", size: 8, color: Muted));

        Container payCell = Labelled(
            "PAYMENT",
            Lines(Muted, "IBAN: DE21 1001 0000 0012 3456 78", "BIC: PBNKDEFFXXX", "Deutsche Postbank, Berlin"),
            Accent);
        payCell.Padding = new Thickness(0, 0, 20, 0);

        Grid bottom = new(1.1f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 20, 0, 0) };
        bottom.AddRow(
            payCell,
            new Container { BackgroundColor = HeaderBg, CornerRadius = 6, Padding = new Thickness(12, 10, 12, 10), Child = xmlStack });
        doc.Add(bottom);


        // ── Bind the invoice XML: PDF/A-3 + embedded CII ─────────────
        byte[] invoiceXml = File.ReadAllBytes(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "factur-x.xml"));
        FacturX.Apply(doc, invoiceXml, FacturXProfile.En16931);

        return doc;
    }

    // ── Kit helpers (mirror InvoiceKit so output matches the TS _kit) ──────────

    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null,
        TextAlignment align = TextAlignment.Left, Thickness? margin = null)
    {
        TextBlock b = new()
        {
            Text = text,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            TextColor = color ?? Ink,
            Alignment = align,
        };
        if (margin.HasValue)
        {
            b.Margin = margin.Value;
        }

        return b;
    }

    private static string Money(decimal value, string symbol, string culture)
        => symbol + value.ToString("N2", CultureInfo.GetCultureInfo(culture));

    // A single TextBlock using hard \n breaks, so an address or payment block
    // tags as one paragraph under PDF/UA.
    private static TextBlock Lines(Color color, params string[] items)
        => Cell(string.Join('\n', items), color: color);

    private static void AddLines(VerticalStack stack, Color color, params string[] items)
        => stack.Add(Lines(color, items));

    private static Container Labelled(string label, IElement body, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label, size: 8f, bold: true, color: accent, margin: new Thickness(0, 0, 0, 5)));
        stack.Add(body);
        return new Container { Child = stack };
    }

    private static HorizontalStack TotalLine(string label, string value, bool bold = false, float size = 9f, Color? color = null)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 3, 0, 3) };
        row.Add(Cell(label, size: size, bold: bold, color: color ?? Muted), 3);
        row.Add(Cell(value, size: size, bold: bold, color: color ?? Ink, align: TextAlignment.Right), 2);
        return row;
    }

    private static Container Divider(Color color, float thickness = 0.6f) => new()
    {
        BackgroundColor = color,
        Height = thickness,
        Margin = new Thickness(0, 5, 0, 5),
    };

    private static Container AccentBar(Color color, float height = 5f) => new()
    {
        BackgroundColor = color,
        Height = height,
    };
}
