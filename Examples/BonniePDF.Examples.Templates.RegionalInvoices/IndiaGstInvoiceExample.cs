using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using static BonniePDF.Examples.Templates.RegionalInvoices.InvoiceKit;

namespace BonniePDF.Examples.Templates.RegionalInvoices;

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
[System.ComponentModel.Description("India GST e-invoice with GSTIN, HSN/SAC codes, CGST/SGST breakdown, IRN and acknowledgement details, and a signed vector QR.")]
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
        doc.Metadata.Author = "Ashwin Technologies Pvt Ltd";
        doc.Metadata.Subject = "India GST e-invoice with IRN, signed QR, HSN codes, and CGST/SGST breakdown.";

        VerticalStack root = new();
        root.AddChild(AccentBar(Saffron, 4));
        root.AddChild(AccentBar(Accent, 4));

        // ── Header: seller | e-invoice signed QR ────────────────────────────
        VerticalStack seller = new();
        seller.AddChild(Cell("TAX INVOICE  ·  e-Invoice", bold: true, size: 8, color: Accent, margin: new Thickness(0, 0, 0, 6)));
        seller.AddChild(Cell("Ashwin Technologies Pvt Ltd", bold: true, size: 15, color: Ink, margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "4th Floor, Prestige Tech Park", "Outer Ring Road, Bengaluru 560103", "Karnataka, India");

        seller.AddChild(Cell("GSTIN: 29ABCDE1234F1Z5", bold: true, color: Accent, margin: new Thickness(0, 6, 0, 0)));
        seller.AddChild(Cell("State: Karnataka (29)  ·  PAN: ABCDE1234F", size: 8, color: Muted));

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
        qrStack.AddChild(qrBox);
        qrStack.AddChild(Cell("Signed QR (IRP)", size: 8, color: Muted, align: TextAlignment.Right, margin: new Thickness(0, 4, 0, 0)));

        Table header = new(1.5f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 14, 0, 14) };
        header.AddRow(new Container { Child = seller }, new Container { Child = qrStack });
        root.AddChild(header);

        // ── IRN band ─────────────────────────────────────────────────────────
        VerticalStack irnStack = new();
        irnStack.AddChild(Cell("IRN (Invoice Reference Number)", bold: true, size: 8, color: Accent, margin: new Thickness(0, 0, 0, 2)));
        irnStack.AddChild(Cell(Irn, size: 8, color: Ink));
        irnStack.AddChild(Cell($"Ack No. 112500771223344   ·   Ack Date {date}", size: 8, color: Muted, margin: new Thickness(0, 3, 0, 0)));
        root.AddChild(new Container
        {
            BackgroundColor = ColorPalette.Emerald[50],
            CornerRadius = 6,
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 16),
            Child = irnStack,
        });

        // ── Buyer + invoice details ─────────────────────────────────────────
        VerticalStack details = new();
        details.AddChild(TotalLine("Invoice no.", $"INV-IN-{year}-0771"));
        details.AddChild(TotalLine("Invoice date", date));
        details.AddChild(TotalLine("Place of supply", "Karnataka (29)"));
        details.AddChild(TotalLine("Reverse charge", "No"));

        Table meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 16),
        };
        meta.AddRow(
            Labelled("BILL TO", Lines(Muted, "Meridian Retail Pvt Ltd", "No. 88, MG Road", "Bengaluru 560001", "Karnataka, India", "GSTIN: 29XYZAB6789K1Z2"), Accent),
            Labelled("DETAILS", details, Accent));
        root.AddChild(meta);

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

        root.AddChild(table);
        root.AddChild(Divider(Rule, 0.5f));

        // ── GST breakdown (intra-state → CGST + SGST) ───────────────────────
        decimal cgst = taxable * Cgst;
        decimal sgst = taxable * Sgst;
        decimal grand = taxable + cgst + sgst;

        VerticalStack totals = new();
        totals.AddChild(TotalLine("Taxable value", Money(taxable, Inr, "en-IN")));
        totals.AddChild(TotalLine("CGST @ 9%", Money(cgst, Inr, "en-IN")));
        totals.AddChild(TotalLine("SGST @ 9%", Money(sgst, Inr, "en-IN")));
        totals.AddChild(Divider(Ink, 0.6f));
        totals.AddChild(TotalLine("Total (INR)", Money(grand, Inr, "en-IN"), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Emerald[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 6, 0, 0) };
        totalsRow.AddChild(new Container(), 1);
        totalsRow.AddChild(totalsBox, 1);
        root.AddChild(totalsRow);

        root.AddChild(Cell(
            "This is a computer-generated e-invoice registered on the GST Invoice Registration Portal. "
                + "The signed QR above encodes the IRP-issued payload.",
            size: 8,
            color: Muted,
            margin: new Thickness(0, 20, 0, 0)));

        doc.Add(root);
        return doc;
    }
}
