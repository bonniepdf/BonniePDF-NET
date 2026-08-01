using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.EdgeReceipt;

/// <summary>
/// Point-of-sale receipt on a custom 80 mm thermal-printer page. The TypeScript
/// equivalent is designed for browser and edge runtimes; this .NET version keeps
/// the document model, native-vector QR code, and Code 128 barcode in parity.
/// </summary>
[System.ComponentModel.DisplayName("Point-of-Sale Receipt")]
[System.ComponentModel.Category("Retail")]
[System.ComponentModel.Description("Point-of-sale receipt on a custom 80mm thermal-printer page, in Courier with a native vector QR digital-receipt link and a Code 128 order barcode. PDF/A-2a + PDF/UA-1.")]
public static class EdgeReceiptExample
{
    private const string OrderNo = "4021";

    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Mute = ColorPalette.Slate[500];

    private static readonly ReceiptItem[] Items =
    [
        new(2, "Flat White", 5.60),
        new(1, "Almond Croissant", 3.20),
        new(1, "Sourdough Loaf", 4.50),
        new(1, "Oat Milk (retail)", 1.95),
    ];

    public static Document Generate()
    {
        DateTime now = DateTime.Now;
        string stamp = now.ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("en-GB"));

        DocumentSettings settings = new(new PdfSize(226.77f, 720, "Receipt 80mm"), 16)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Courier").WithSize(9).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = $"Receipt #{OrderNo} - Sample Coffee";
        doc.Metadata.Author = "Sample Coffee";
        doc.Metadata.Subject = "Point-of-sale receipt generated locally with BonniePDF.";

        doc.Add(new Heading("SAMPLE COFFEE")
        {
            Level = 1,
            FontName = StandardFonts.CourierBold,
            FontSize = 13,
            TextColor = Ink,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3),
        });
        doc.Add(Center("123 Harbour Road, Bristol BS1 4RW"));
        doc.Add(Center("+44 117 555 0110"));
        doc.Add(Rule());

        doc.Add(Kv("Order", "#" + OrderNo));
        doc.Add(Kv("Date", stamp));
        doc.Add(Kv("Served", "Register 3 - Alex"));
        doc.Add(Rule());

        double total = 0;
        foreach (ReceiptItem item in Items)
        {
            total += item.Price;
            doc.Add(Kv($"{item.Quantity} x {item.Name}", Money(item.Price)));
        }

        doc.Add(Rule());

        double vat = total - total / 1.2;
        doc.Add(Kv("Subtotal", Money(total - vat)));
        doc.Add(Kv("VAT (20%)", Money(vat)));
        doc.Add(Kv("TOTAL", Money(total), bold: true, size: 12));
        doc.Add(Rule());

        doc.Add(Kv("Card **** 4242", Money(total)));
        doc.Add(Kv("Approved", "AUTH 06Q41X"));
        doc.Add(Rule());

        QRCode qr = new()
        {
            Data = $"https://receipts.example.com/r/{OrderNo}",
            Size = 108,
            QuietZone = 1,
            ErrorCorrection = QrErrorCorrection.Medium,
            ForegroundColor = Ink,
            AltText = $"QR code linking to the digital receipt for order {OrderNo}.",
        };
        doc.Add(new Container
        {
            Child = qr,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0, 4, 0, 4),
        });
        doc.Add(Center("Scan for your digital receipt", 8, Mute));

        Barcode barcode = new()
        {
            Value = "NW" + OrderNo,
            Symbology = BarcodeSymbology.Code128,
            Height = 38,
            AltText = $"Order barcode (Code 128): NW{OrderNo}",
        };
        doc.Add(new Container
        {
            Child = barcode,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0, 8, 0, 2),
        });

        doc.Add(Rule());
        doc.Add(Center("Thank you! Returns within 30 days", 8, Ink));
        doc.Add(Center("with your receipt.", 8, Mute));
        doc.Add(Center("Generated with BonniePDF", 7, Mute, 8));

        return doc;
    }

    private static string Money(double amount) => "GBP " + amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static TextBlock Center(string text, float size = 8, Color? color = null, float marginTop = 0)
    {
        TextBlock block = new()
        {
            Text = text,
            FontName = StandardFonts.Courier,
            FontSize = size,
            TextColor = color ?? Mute,
            Alignment = TextAlignment.Center,
        };
        if (marginTop > 0)
        {
            block.Margin = new Thickness(0, marginTop, 0, 0);
        }

        return block;
    }

    private static HorizontalStack Kv(string label, string value, bool bold = false, float size = 9)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 1.5f, 0, 1.5f),
        };
        row.AddChild(Cell(label, size, bold, TextAlignment.Left), 3);
        row.AddChild(Cell(value, size, bold, TextAlignment.Right), 2);
        return row;
    }

    private static TextBlock Cell(string text, float size, bool bold, TextAlignment align) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.CourierBold : StandardFonts.Courier,
        FontSize = size,
        TextColor = Ink,
        Alignment = align,
    };

    private static TextBlock Rule() => new()
    {
        Text = "- - - - - - - - - - - - - - - -",
        FontName = StandardFonts.Courier,
        FontSize = 9,
        TextColor = Mute,
        Alignment = TextAlignment.Center,
        Margin = new Thickness(0, 5, 0, 5),
    };

    private sealed record ReceiptItem(int Quantity, string Name, double Price);
}
