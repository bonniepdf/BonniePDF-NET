using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Invoices.RegionalInvoices;

/// <summary>
/// India GST e-invoice (IRN + signed QR). India's GST e-invoicing system routes
/// every B2B invoice through the Invoice Registration Portal (IRP), which returns
/// an Invoice Reference Number (IRN) and a digitally-signed QR code that must be
/// printed on the invoice. This renders that idiom: both parties' GSTIN, HSN/SAC
/// codes per line, an intra-state CGST + SGST split (9% + 9%), the IRN and
/// acknowledgement details, INR amounts, and the signed QR generated with
/// BonniePDF's native vector QR. Fictional GSTIN / IRN values.
/// </summary>
[System.ComponentModel.DisplayName("India GST E-Invoice")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description(
    "India GST e-invoice with GSTIN, HSN/SAC codes, CGST/SGST breakdown, IRN and acknowledgement details, and a signed vector QR.")]
public static class IndiaGstInvoiceExample
{
    private static readonly Color Accent = ColorPalette.Emerald[700];
    private static readonly Color Saffron = ColorPalette.Amber[500];

    // Helvetica (standard-14) has no ₹ glyph; use the ASCII abbreviation.
    private const string Inr = "Rs ";

    private const decimal Cgst = 0.09m;
    private const decimal Sgst = 0.09m;
    private const string Irn = "8a7f1e2c9b4d6f03a1e5c7b90d2f4a6e8c1b3d5f70921a4c6e8b0d2f4a6c8e1b3";

    private sealed record GstLine(string Description, string Hsn, int Qty, decimal UnitPrice);

    private static readonly GstLine[] Items =
    [
        new("Enterprise SaaS subscription", "997331", 12, 18000m),
        new("Implementation & configuration", "998314", 40, 2500m),
        new("Priority support (annual)", "998313", 1, 96000m),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        DateTime now = DateTime.Now;
        int year = now.Year;
        string date = now.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-IN"));

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 40 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-IN";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);

        doc.Metadata.Title = $"GST e-Invoice INV-IN-{year}-0771";
        doc.Metadata.Author = "Sample Company Private Limited";
        doc.Metadata.Subject = "India GST e-invoice with IRN, signed QR, HSN codes, and CGST/SGST breakdown.";

        doc.Add(AccentBar(Saffron, 4));
        doc.Add(AccentBar(Accent, 4));

        // ── Header: seller | e-invoice signed QR ────────────────────────────
        VerticalStack seller = new();
        seller.Add(Cell("TAX INVOICE  ·  e-Invoice", bold: true, size: 8, color: Accent,
            margin: new Thickness(0, 0, 0, 6)));
        seller.Add(Cell("Sample Company Private Limited", bold: true, size: 15, color: Ink,
            margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "4th Floor, Prestige Tech Park", "Outer Ring Road, Bengaluru 560103",
            "Karnataka, India");

        seller.Add(Cell("GSTIN: 29ABCDE1234F1Z5", bold: true, color: Accent, margin: new Thickness(0, 6, 0, 0)));
        seller.Add(Cell("State: Karnataka (29)  ·  PAN: ABCDE1234F", size: 8, color: Muted));

        // Signed QR: on a real e-invoice this encodes the IRP-signed payload (JWT).
        string qrPayload = $"{{\"SellerGstin\":\"29ABCDE1234F1Z5\",\"BuyerGstin\":\"29XYZAB6789K1Z2\","
                           + $"\"DocNo\":\"INV-IN-{year}-0771\",\"DocDt\":\"{date}\",\"Irn\":\"{Irn}\"}}";
        Container qrBox = new()
        {
            HorizontalAlignment = HorizontalAlignment.End,
            Child = new QRCode
            {
                Data = qrPayload,
                Size = 108,
                QuietZone = 2,
                ErrorCorrection = QrErrorCorrection.Medium,
                ForegroundColor = Ink,
                BackgroundColor = Color.White,
                AltText = "Signed GST e-invoice QR code issued by the Invoice Registration Portal.",
            },
        };
        VerticalStack qrStack = new();
        qrStack.Add(qrBox);
        qrStack.Add(Cell("Signed QR (IRP)", size: 8, color: Muted, align: TextAlignment.Right,
            margin: new Thickness(0, 4, 0, 0)));

        Grid header = new(1.5f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 14, 0, 14) };
        header.AddRow(seller, qrStack);
        doc.Add(header);

        // ── IRN band ─────────────────────────────────────────────────────────
        VerticalStack irnStack = new();
        irnStack.Add(Cell("IRN (Invoice Reference Number)", bold: true, size: 8, color: Accent,
            margin: new Thickness(0, 0, 0, 2)));
        irnStack.Add(Cell(Irn, size: 8, color: Ink));
        irnStack.Add(Cell($"Ack No. 112500771223344   ·   Ack Date {date}", size: 8, color: Muted,
            margin: new Thickness(0, 3, 0, 0)));
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Emerald[50],
            CornerRadius = 6,
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 16),
            Child = irnStack,
        });

        // ── Buyer + invoice details ─────────────────────────────────────────
        VerticalStack details = new();
        details.Add(TotalLine("Invoice no.", $"INV-IN-{year}-0771"));
        details.Add(TotalLine("Invoice date", date));
        details.Add(TotalLine("Place of supply", "Karnataka (29)"));
        details.Add(TotalLine("Reverse charge", "No"));

        Grid meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 16),
        };
        meta.AddRow(
            Labelled("BILL TO",
                Lines(Muted, "Sample Customer Private Limited", "No. 88, MG Road", "Bengaluru 560001",
                    "Karnataka, India", "GSTIN: 29XYZAB6789K1Z2"), Accent),
            Labelled("DETAILS", details, Accent));
        doc.Add(meta);

        // ── Line items with HSN + taxable value ─────────────────────────────
        Table table = new(2.6f, 0.9f, 0.5f, 1f, 1f)
        {
            CellPadding = 7,
            Border = Border.None,
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
        };
        table.AddHeader(
            Cell("Description", bold: true),
            Cell("HSN/SAC", bold: true, align: TextAlignment.Center),
            Cell("Qty", bold: true, align: TextAlignment.Center),
            Cell("Rate", bold: true, align: TextAlignment.Right),
            Cell("Taxable value", bold: true, align: TextAlignment.Right));

        decimal taxable = 0m;
        foreach (GstLine it in Items)
        {
            decimal value = it.Qty * it.UnitPrice;
            taxable += value;
            table.AddRow(
                Cell(it.Description),
                Cell(it.Hsn, align: TextAlignment.Center, color: Muted),
                Cell(it.Qty.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Center),
                Cell(Money(it.UnitPrice, Inr, "en-IN"), align: TextAlignment.Right),
                Cell(Money(value, Inr, "en-IN"), align: TextAlignment.Right));
        }

        doc.Add(table);
        doc.Add(Divider(Rule, 0.5f));

        // ── GST breakdown (intra-state → CGST + SGST) ───────────────────────
        decimal cgst = taxable * Cgst;
        decimal sgst = taxable * Sgst;
        decimal grand = taxable + cgst + sgst;

        VerticalStack totals = new();
        totals.Add(TotalLine("Taxable value", Money(taxable, Inr, "en-IN")));
        totals.Add(TotalLine("CGST @ 9%", Money(cgst, Inr, "en-IN")));
        totals.Add(TotalLine("SGST @ 9%", Money(sgst, Inr, "en-IN")));
        totals.Add(Divider(Ink, 0.6f));
        totals.Add(TotalLine("Total (INR)", Money(grand, Inr, "en-IN"), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Emerald[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new()
            { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 6, 0, 0) };
        totalsRow.Add(new Container(), 1);
        totalsRow.Add(totalsBox, 1);
        doc.Add(totalsRow);

        doc.Add(Cell(
            "This is a computer-generated e-invoice registered on the GST Invoice Registration Portal. "
            + "The signed QR above encodes the IRP-issued payload.",
            size: 8,
            color: Muted,
            margin: new Thickness(0, 20, 0, 0)));

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
}
