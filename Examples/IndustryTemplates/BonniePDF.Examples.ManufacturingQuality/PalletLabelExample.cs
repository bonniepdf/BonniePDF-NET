using BonniePDF;

namespace BonniePDF.Examples.ManufacturingQuality;

/// <summary>
/// An A6 pallet label for a batch of ingredients: the shipper and consignee, product, batch and dates, a
/// serial shipping container code and an ITF-14 carton barcode, a DataMatrix of the product, batch and
/// expiry, and handling text set vertically along both edges.
/// </summary>
[System.ComponentModel.DisplayName("Pallet Label")]
[System.ComponentModel.Category("Manufacturing & Quality")]
[System.ComponentModel.Description("An A6 pallet label for a batch of ingredients: the shipper and consignee, product, batch and dates, a serial shipping container code and an ITF-14 carton barcode, a DataMatrix of the product, batch and expiry, and handling text set vertically along both edges.")]
public static class PalletLabelExample
{
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Mute = ColorPalette.Stone[600];
    private static readonly Color Accent = ColorPalette.Orange[800];

    /// <summary>A6 portrait, 105 × 148 mm, in points.</summary>
    private static readonly PdfSize A6 = new(297.6f, 419.5f, "A6");

    /// <summary>The height the edge strips fill: the page less its margins, with a point to spare.</summary>
    private const float StripHeight = 398;

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Document doc = new(
            new DocumentSettings
            {
                PageSize = A6,
                Margin = 10,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(7.5f).WithColor(Ink),
            },
            new DocumentMetadata
            {
                Title = "Pallet label: batch 24-1187, pallet 1 of 3",
                Author = "Sample Ingredients Ltd",
                Subject = "Logistic label for a pallet of citric acid",
            });

        // ── Shipper and consignee ──
        Grid parties = new(1, 1) { Border = new Border(0, 0, 0, 0.75f), BorderColors = new BorderColors(Ink), CellPadding = 4 };
        parties.AddRow(
            Party("From", "Sample Ingredients Ltd\nUnit 4, Sample Industrial Estate\nSample Town ST6 1AB"),
            Party("To", "Sample Beverages Ltd\nGoods in, door 3\nSample City SC2 9XY"));

        // ── Product and the facts a warehouse needs ──
        VerticalStack product = new() { Spacing = 1, Margin = new Thickness(0, 6, 0, 4) };
        product.Add(Tb("PRODUCT", 6, Mute, bold: true));
        product.Add(new Heading("Citric acid anhydrous, food grade")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            Color = Ink,
            Margin = new Thickness(0),
        });
        product.Add(Tb("40 bags of 25 kg  ·  Order PO-55-3021", 8, Ink));
        Grid facts = new(1, 1, 1) { Border = new Border(0, 0, 0, 0.5f), BorderColors = new BorderColors(ColorPalette.Stone[300]), CellPadding = 3 };
        facts.AddRow(Fact("Batch", "24-1187"), Fact("Best before", "13 Oct 2029"), Fact("Pallet", "1 of 3"));
        facts.AddRow(Fact("Net weight", "1,000 kg"), Fact("Gross weight", "1,045 kg"), Fact("Dispatched", "5 Nov 2026"));

        // ── Codes ──
        HorizontalStack codes = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 8, Margin = new Thickness(0, 6, 0, 0) };
        VerticalStack carton = new() { Spacing = 2 };
        carton.Add(Tb("CASE GTIN (ITF-14)", 6, Mute, bold: true));
        carton.Add(new Barcode
        {
            Value = "1506000000000",
            Symbology = BarcodeSymbology.Itf14,
            Height = 40,
            ModuleWidth = 1,
            ForegroundColor = Ink,
            BackgroundColor = Color.White,
            TextLabelFontSize = 6.5f,
            AltText = "ITF-14 barcode of the case product number 1506000000000 with its check digit",
        });
        codes.Add(carton, 1);
        VerticalStack matrix = new() { Spacing = 2 };
        matrix.Add(Tb("PRODUCT, BATCH, EXPIRY", 6, Mute, bold: true));
        matrix.Add(new DataMatrix
        {
            Data = "0115060000000006172910131024-1187",
            Gs1 = true,
            Size = 46,
            QuietZone = 1,
            ForegroundColor = Ink,
            AltText = "DataMatrix code: product 15060000000006, batch 24-1187, expiry 13 October 2029",
        });
        codes.Add(matrix);
        VerticalStack sscc = new() { Spacing = 2, Margin = new Thickness(0, 12, 0, 0) };
        sscc.Add(Tb("SERIAL SHIPPING CONTAINER CODE", 6, Mute, bold: true));
        sscc.Add(new Barcode
        {
            Value = "00350600000000000017",
            Symbology = BarcodeSymbology.Code128,
            Gs1 = true,
            Height = 72,
            ModuleWidth = 0.95f,
            ForegroundColor = Ink,
            BackgroundColor = Color.White,
            TextLabelFontSize = 7,
            AltText = "GS1-128 barcode of the serial shipping container code 350600000000000017",
        });

        VerticalStack body = new();
        body.Add(parties);
        body.Add(product);
        body.Add(facts);
        body.Add(codes);
        body.Add(sscc);

        // ── The label: handling text up the left edge, the body, and the batch down the right edge ──
        HorizontalStack label = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 6 };
        label.Add(Edge("THIS WAY UP  ·  DO NOT STACK MORE THAN TWO HIGH  ·  KEEP DRY", 270, Accent, Color.White));
        label.Add(body, 1);
        label.Add(Edge("BATCH 24-1187  ·  BEST BEFORE 13 OCT 2029", 90, ColorPalette.Stone[100], Ink));
        doc.Add(label);
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Text in Helvetica or Helvetica Bold.</summary>
    private static TextBlock Tb(string text, float size, Color color, bool bold = false) => new(text)
    {
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color,
    };

    /// <summary>A labelled address block.</summary>
    private static VerticalStack Party(string label, string address)
    {
        VerticalStack stack = new() { Spacing = 1 };
        stack.Add(Tb(label.ToUpperInvariant(), 6, Mute, bold: true));
        stack.Add(Tb(address, 7.5f, Ink));
        return stack;
    }

    /// <summary>A labelled fact.</summary>
    private static VerticalStack Fact(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 1 };
        stack.Add(Tb(label.ToUpperInvariant(), 6, Mute, bold: true));
        stack.Add(Tb(value, 9.5f, Ink, bold: true));
        return stack;
    }

    /// <summary>A strip down one edge with text turned a quarter turn: 270° reads upwards, 90° reads downwards.</summary>
    private static Container Edge(string text, float angle, Color fill, Color ink) => new()
    {
        BackgroundColor = fill,
        Width = 18,
        Height = StripHeight,
        Padding = new Thickness(4, 0, 0, 0),
        Child = new Rotate
        {
            Angle = angle,
            Mode = RotationMode.Constrained,
            ContentWidth = StripHeight,
            Child = new TextBlock(text)
            {
                FontName = StandardFonts.HelveticaBold,
                FontSize = 7,
                LetterSpacing = 1.5f,
                Color = ink,
                Alignment = TextAlignment.Center,
            },
        },
    };
}
