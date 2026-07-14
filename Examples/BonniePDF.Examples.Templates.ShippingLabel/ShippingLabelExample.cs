using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.ShippingLabel;

/// <summary>
/// Carrier-style 4x6in shipping label: PDF/A-2a + PDF/UA-1 with a Code 128
/// tracking barcode, an ITF-14 carton code, and a QR parcel link - each with
/// alt text. Demonstrates native vector barcodes and exact label sizing on a
/// custom <see cref="PdfSize"/>. Fictional carrier and codes only.
/// </summary>
[System.ComponentModel.DisplayName("Shipping Label")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Carrier-style 4x6in shipping label (PDF/UA-1 + PDF/A-2a) with a Code 128 tracking barcode, an ITF-14 carton code, and a QR parcel link - native vector barcodes, exact label sizing.")]
public static class ShippingLabelExample
{
    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color BodyCol = ColorPalette.Slate[600];
    private static readonly Color Mute = ColorPalette.Slate[500];
    private static readonly Color LineCol = ColorPalette.Slate[300];

    public static Document Generate()
    {
        // 4 x 6 inch label = 288 x 432 pt.
        Document doc = new(new DocumentSettings
        {
            PageSize = new PdfSize(288, 432, "Shipping Label 4x6"),
            Margin = 12,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8).WithColor(BodyCol),
        });

        doc.Metadata.Title = "Shipping Label 1Z999AA10123456784";
        doc.Metadata.Author = "Acme Logistics";
        doc.Metadata.Subject = "Carrier-style shipping label - accessible (PDF/UA) and archival (PDF/A-2a)";

        Heading heading = new("Shipping label")
        {
            Level = 1,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 6),
        };
        doc.Add(heading);

        // ── Carrier header + service class ────────────────────────────────────
        doc.Add(Band("ACME LOGISTICS", 12, Ink, Color.White));

        Grid svcRow = new(1f, 1f) { Border = Border.None, CellPadding = 0 };
        svcRow.AddRow(
            new Container { Child = Tb("EXPRESS - NEXT DAY", 10, Color.White, bold: true) },
            new Container { Child = Tb("1.4 kg", 10, Color.White, bold: true, align: TextAlignment.Right) });
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[800],
            Padding = new Thickness(10, 4, 10, 4),
            Child = svcRow,
        });

        doc.Add(HRule(5, 4));

        // ── From / To addresses ───────────────────────────────────────────────
        VerticalStack from = new();
        from.AddChild(Tb("FROM", 7, Mute, bold: true, bottom: 2));
        from.AddChild(Tb("Northwind Supplies", 8, Ink, bold: true));
        from.AddChild(Tb("4 Tenter Ground", 8, BodyCol));
        from.AddChild(Tb("London E1 7NH, UK", 8, BodyCol));
        doc.Add(new Container { Child = from, Margin = new Thickness(0, 0, 0, 4) });

        VerticalStack to = new();
        to.AddChild(Tb("SHIP TO", 8, Mute, bold: true, bottom: 3));
        to.AddChild(Tb("Jordan Avery", 13, Ink, bold: true));
        to.AddChild(Tb("Aperture Labs Inc.", 10, Ink));
        to.AddChild(Tb("19 Enfield Road", 10, Ink));
        to.AddChild(Tb("London N1 5BA", 10, Ink));
        to.AddChild(Tb("United Kingdom", 10, Ink));
        doc.Add(new Container
        {
            Child = to,
            Border = new Border(1),
            BorderColors = new BorderColors(Ink),
            Padding = new Thickness(9, 6, 9, 6),
        });

        // ── Routing band (postcode zone) ──────────────────────────────────────
        doc.Add(Band("N1  -  ZONE 24  -  LON", 13, Ink, Color.White, TextAlignment.Center));

        doc.Add(HRule(5, 4));

        // ── Tracking barcode (Code 128) ───────────────────────────────────────
        doc.Add(Tb("TRACKING NO.", 7, Mute, bold: true, bottom: 2));
        Barcode tracking = new()
        {
            Value = "1Z999AA10123456784",
            Symbology = BarcodeSymbology.Code128,
            Height = 46,
            AltText = "Tracking barcode (Code 128): 1Z999AA10123456784",
        };
        doc.Add(new Container { Child = tracking, Padding = new Thickness(4, 4, 4, 2) });

        doc.Add(HRule(5, 4));

        // ── Carton code (ITF-14) + QR parcel link, side by side ───────────────
        VerticalStack cartonStack = new();
        cartonStack.AddChild(Tb("CARTON (ITF-14)", 7, Mute, bold: true, bottom: 2));
        cartonStack.AddChild(new Container
        {
            Child = new Barcode
            {
                Value = "1540141253721", // 13 digits + auto check = ITF-14
                Symbology = BarcodeSymbology.Itf14,
                Height = 40,
                AltText = "Carton code (ITF-14): 1540141253721",
            },
        });

        VerticalStack qrStack = new();
        qrStack.AddChild(Tb("SCAN", 7, Mute, bold: true, bottom: 2, align: TextAlignment.Right));
        qrStack.AddChild(new Container
        {
            HorizontalAlignment = HorizontalAlignment.End,
            Child = new QRCode
            {
                Data = "https://track.acme.example/p/1Z999AA10123456784",
                Size = 66,
                QuietZone = 1,
                ErrorCorrection = QrErrorCorrection.Quartile,
                AltText = "QR code linking to parcel tracking for 1Z999AA10123456784",
            },
        });

        Grid bottom = new(2f, 1f) { Border = Border.None, CellPadding = 0 };
        bottom.AddRow(cartonStack, qrStack);
        doc.Add(bottom);

        return doc;
    }

    // A full-bleed coloured band with bold text (carrier header / routing).
    private static Container Band(string text, float size, Color bg, Color fg, TextAlignment align = TextAlignment.Left) => new()
    {
        BackgroundColor = bg,
        Padding = new Thickness(10, 5, 10, 5),
        Child = Tb(text, size, fg, bold: true, align: align),
    };

    private static Container HRule(float top, float bottom) => new()
    {
        BackgroundColor = LineCol,
        Child = new TextBlock { Text = " ", FontSize = 0.5f },
        Padding = new Thickness(0, 0.5f, 0, 0.5f),
        Margin = new Thickness(0, top, 0, bottom),
    };

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
}
