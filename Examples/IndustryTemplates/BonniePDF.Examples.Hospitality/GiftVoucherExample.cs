using BonniePDF;

namespace BonniePDF.Examples.Hospitality;

/// <summary>
/// A restaurant gift voucher on a DL page: the value in large serif type, the recipient's name, a serial
/// barcode, and a perforated stub the restaurant keeps, with a QR code to check the balance.
/// </summary>
[System.ComponentModel.DisplayName("Gift Voucher")]
[System.ComponentModel.Category("Hospitality")]
[System.ComponentModel.Description("A restaurant gift voucher on a DL page: the value in large serif type, the recipient’s name, a serial barcode, and a perforated stub the restaurant keeps, with a QR code to check the balance.")]
public static class GiftVoucherExample
{
    private const string SerifKey = "fonts/SourceSerif4-Regular.ttf";
    private const string Serial = "LRD-100-4821-7730";

    /// <summary>A DL page, 210 × 99 mm, in points.</summary>
    private static readonly PdfSize Dl = new(595.3f, 280.6f, "DL");

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = Dl,
                Orientation = PageOrientation.Landscape,
                Margin = 0,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(ColorPalette.Stone[700]),
            },
            Metadata = new DocumentMetadata
            {
                Title = "The Larder gift voucher",
                Author = "The Larder",
                Subject = $"Gift voucher {Serial}, £100",
            },
        };
        if (!assets.TryGetValue(SerifKey, out byte[]? bytes))
            throw new InvalidOperationException($"Supply {SerifKey}.");
        string serif = doc.Fonts.Add(bytes).FontName;

        // ── The voucher: dark, with the value large ──
        HorizontalStack serialRow = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 12 };
        serialRow.Add(new Container
        {
            BackgroundColor = Color.White,
            CornerRadius = 3,
            Padding = new Thickness(6, 4, 6, 4),
            Child = new Barcode
            {
                Value = Serial,
                Symbology = BarcodeSymbology.Code128,
                Height = 22,
                ModuleWidth = 0.8f,
                ForegroundColor = ColorPalette.Stone[900],
                BackgroundColor = Color.White,
                TextLabelFontSize = 6,
                AltText = $"Voucher serial number barcode: {Serial}",
            },
        });
        serialRow.Add(new TextBlock($"Valid until 5 November 2027\nSerial {Serial}") { FontSize = 7.5f, Color = ColorPalette.Stone[400] });

        VerticalStack face = new() { Spacing = 4 };
        face.Add(new TextBlock("THE LARDER  ·  GIFT VOUCHER")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            LetterSpacing = 2.5f,
            Color = ColorPalette.Amber[300],
            Margin = new Thickness(0, 0, 0, 10),
        });
        face.Add(new Heading("A gift for dinner") { Level = 1, FontName = serif, FontSize = 28, Color = Color.White, Margin = new Thickness(0) });
        face.Add(new TextBlock("£100") { FontName = serif, FontSize = 46, Color = ColorPalette.Amber[200] });
        face.Add(new TextBlock("to spend on food and drink, at lunch or dinner, any day we are open")
        {
            FontSize = 9,
            Color = ColorPalette.Stone[300],
            Margin = new Thickness(0, 0, 0, 12),
        });
        face.Add(new TextBlock("For Priya Shah, with love from Tom") { FontName = serif, FontSize = 13, Color = Color.White, Margin = new Thickness(0, 0, 0, 14) });
        face.Add(serialRow);
        Container voucher = new()
        {
            BackgroundColor = ColorPalette.Stone[900],
            Height = Dl.Height,
            Padding = new Thickness(30, 26, 30, 26),
            Child = face,
        };

        // ── The stub the restaurant keeps ──
        VerticalStack stubText = new() { Spacing = 3 };
        stubText.Add(new TextBlock("RESTAURANT COPY")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7,
            LetterSpacing = 1.2f,
            Color = ColorPalette.Amber[800],
            Margin = new Thickness(0, 0, 0, 8),
        });
        foreach (var (label, value) in new[] { ("Serial", Serial), ("Value", "£100.00"), ("Issued", "5 November 2026"), ("Expires", "5 November 2027") })
        {
            VerticalStack field = new() { Margin = new Thickness(0, 0, 0, 4) };
            field.Add(new TextBlock(label) { FontSize = 6.5f, Color = ColorPalette.Stone[500] });
            field.Add(new TextBlock(value) { FontName = StandardFonts.HelveticaBold, FontSize = 8.5f, Color = ColorPalette.Stone[900] });
            stubText.Add(field);
        }
        stubText.Add(new QRCode
        {
            Data = $"https://thelarder.example/v/{Serial}",
            Size = 54,
            QuietZone = 1,
            ForegroundColor = ColorPalette.Stone[900],
            AltText = $"QR code: check the balance of voucher {Serial}",
            Margin = new Thickness(0, 4, 0, 0),
        });
        stubText.Add(new TextBlock("Scan to check the balance") { FontSize = 6.5f, Color = ColorPalette.Stone[500] });
        Container stub = new()
        {
            Height = Dl.Height,
            Padding = new Thickness(16, 26, 16, 26),
            Child = stubText,
        };

        HorizontalStack sheet = new() { WidthDistribution = WidthDistribution.Weighted };
        sheet.Add(new Container { BackgroundColor = ColorPalette.Amber[700], Width = 8, Height = Dl.Height });
        sheet.Add(voucher, 3.2f);
        sheet.Add(Perforation());
        sheet.Add(stub, 1);
        doc.Add(sheet);
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>The tear line between the voucher and its stub: a column of small dots with a notch at each end.</summary>
    private static CanvasElement Perforation() => new((canvas, width, height) =>
    {
        float x = width / 2;
        canvas.FillCircle(x, 0, 6, ColorPalette.Stone[300]);
        canvas.FillCircle(x, height, 6, ColorPalette.Stone[300]);
        for (float y = 12; y < height - 10; y += 6) canvas.FillCircle(x, y, 1, ColorPalette.Stone[300]);
    })
    {
        DrawWidth = 14,
        DrawHeight = Dl.Height,
        IsDecorative = true,
    };
}
