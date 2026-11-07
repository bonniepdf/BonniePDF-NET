using BonniePDF;

namespace BonniePDF.Examples.ManufacturingQuality;

/// <summary>
/// A certificate of analysis for one batch of a food ingredient: a vertical title running up the edge of
/// the page, the batch details, every test against its specification and method, a release statement, and
/// a QR code to verify the certificate.
/// </summary>
[System.ComponentModel.DisplayName("Certificate of Analysis")]
[System.ComponentModel.Category("Manufacturing & Quality")]
[System.ComponentModel.Description("A certificate of analysis for one batch of a food ingredient: a vertical title running up the edge of the page, the batch details, every test against its specification and method, a release statement, and a QR code to verify the certificate.")]
public static class CertificateOfAnalysisExample
{
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Body = ColorPalette.Stone[700];
    private static readonly Color Mute = ColorPalette.Stone[600];
    private static readonly Color Accent = ColorPalette.Orange[800];

    /// <summary>The tests: name, method, specification, and the result for this batch. Every result conforms.</summary>
    private static readonly (string Test, string Method, string Specification, string Result)[] Tests =
    [
        ("Appearance", "Visual", "White crystalline powder", "Conforms"),
        ("Identification", "IR spectrum", "Matches reference", "Conforms"),
        ("Assay, anhydrous basis", "Titration", "99.5 to 100.5%", "99.8%"),
        ("Water", "Karl Fischer", "0.5% max", "0.12%"),
        ("Sulfated ash", "Gravimetric", "0.05% max", "0.02%"),
        ("Oxalate", "Limit test", "100 mg/kg max", "below 50 mg/kg"),
        ("Sulfate", "Limit test", "150 mg/kg max", "40 mg/kg"),
        ("Lead", "ICP-MS", "0.5 mg/kg max", "below 0.1 mg/kg"),
        ("Arsenic", "ICP-MS", "1 mg/kg max", "below 0.1 mg/kg"),
        ("Particle size, through 30 mesh", "Sieve", "90% min", "96%"),
        ("Total aerobic microbial count", "ISO 4833-1", "1,000 cfu/g max", "below 10 cfu/g"),
        ("Yeasts and moulds", "ISO 21527-2", "100 cfu/g max", "below 10 cfu/g"),
    ];

    /// <summary>Declarations the customer's own food safety team asks for.</summary>
    private static readonly (string Label, string Text)[] Declarations =
    [
        ("Allergens", "Contains none of the 14 allergens listed in Regulation (EU) 1169/2011"),
        ("Genetic modification", "Not produced from genetically modified organisms"),
        ("Irradiation", "Not irradiated"),
        ("Origin", "Produced by fermentation in the EU"),
    ];

    /// <summary>The batch at a glance.</summary>
    private static readonly (string Label, string Value)[] Batch =
    [
        ("Product", "Citric acid anhydrous, food grade (E330)"),
        ("Product code", "CA-ANH-25"),
        ("Batch", "24-1187"),
        ("Quantity", "1,000 kg in 40 bags of 25 kg"),
        ("Manufactured", "14 October 2026"),
        ("Retest or expiry", "13 October 2029"),
        ("Customer", "Sample Beverages Ltd, order PO-55-3021"),
        ("Storage", "Dry, below 25 °C, away from strong bases"),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Container band = EdgeBand();
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 42,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Body),
                // A band down the left edge carries the document's title, turned to read upwards.
                PageBackground = _ => band,
                FooterTemplate = PageTemplate.FromElements(@default: new TextBlock(
                    "Sample Ingredients Ltd  ·  Unit 4, Sample Industrial Estate, Sample Town ST6 1AB  ·  Certificate CoA-24-1187")
                {
                    FontSize = 7,
                    Color = Mute,
                }),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Certificate of analysis: batch 24-1187",
                Author = "Sample Ingredients Ltd",
                Subject = "Certificate of analysis for citric acid anhydrous, batch 24-1187",
            },
        };

        // ── Title ──
        HorizontalStack top = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 16, Margin = new Thickness(0, 0, 0, 14) };
        VerticalStack title = new() { Spacing = 3 };
        title.Add(Tb("SAMPLE INGREDIENTS LTD  ·  QUALITY CONTROL", 7.5f, Accent, bold: true));
        title.Add(new Heading("Certificate of analysis")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Ink,
            Margin = new Thickness(0),
        });
        title.Add(Tb("Certificate CoA-24-1187, issued 5 November 2026", 9, Mute));
        top.Add(title, 1);
        VerticalStack status = new() { Spacing = 2 };
        status.Add(Tb("BATCH STATUS", 6.5f, ColorPalette.Emerald[800], bold: true));
        status.Add(Tb("Released", 14, ColorPalette.Emerald[900], bold: true));
        top.Add(new Container
        {
            BackgroundColor = ColorPalette.Emerald[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Emerald[300]),
            CornerRadius = 6,
            Padding = new Thickness(12, 8, 12, 8),
            Child = status,
        });
        doc.Add(top);

        // ── Batch details ──
        Grid details = new(1, 2.6f)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            Margin = new Thickness(0, 0, 0, 16),
        };
        foreach (var (label, value) in Batch)
        {
            details.AddRow(new TableRow
            {
                Cells =
                [
                    new TableCell { Content = Tb(label, 8.5f, Mute), BackgroundColor = ColorPalette.Stone[50] },
                    new TableCell { Content = Tb(value, 9, Ink, bold: label is "Batch" or "Product") },
                ],
            });
        }
        doc.Add(Section("Batch"));
        doc.Add(details);

        // ── Results ──
        doc.Add(Section("Results"));
        Table results = new(2.1f, 1.2f, 1.6f, 1.3f, 0.8f)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[800],
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 16),
        };
        results.AddHeader(Th("Test"), Th("Method"), Th("Specification"), Th("Result"), Th("Status"));
        foreach (var (test, method, specification, result) in Tests)
            results.AddRow(Tb(test, 8.5f, Ink), Tb(method, 8.5f, Mute), Tb(specification, 8.5f, Body), Tb(result, 8.5f, Ink, bold: true), Pass());
        doc.Add(results);

        // ── Declarations ──
        doc.Add(Section("Declarations"));
        Grid declarations = new(1, 1) { CellPadding = 0, Margin = new Thickness(0, 0, 0, 16) };
        for (int i = 0; i < Declarations.Length; i += 2)
            declarations.AddRow(Declarations.Skip(i).Take(2).Select(d => (IElement)Declaration(d.Label, d.Text)).ToArray());
        doc.Add(declarations);

        // ── Release statement, signature and verification ──
        HorizontalStack release = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 24 };
        VerticalStack statement = new() { Spacing = 6 };
        statement.Add(Tb("We certify that batch 24-1187 was manufactured under our food safety management system (FSSC 22000) and conforms to the specification above, to EU Regulation 231/2012 for E330, and to the Food Chemicals Codex monograph for citric acid.", 8.5f, Body));
        statement.Add(new Container { Height = 26, Border = new Border(0, 0, 0, 0.75f), BorderColors = new BorderColors(ColorPalette.Stone[400]) });
        statement.Add(Tb("Dr H. Lindqvist, quality assurance manager  ·  5 November 2026", 8, Mute));
        release.Add(statement, 1);
        VerticalStack verify = new() { Spacing = 3 };
        verify.Add(new QRCode
        {
            Data = "https://ingredients.example/coa/24-1187",
            Size = 64,
            QuietZone = 1,
            ForegroundColor = Ink,
            AltText = "QR code: verify certificate CoA-24-1187 online",
        });
        verify.Add(Tb("Verify this certificate", 7, Mute));
        release.Add(verify);
        doc.Add(release);
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

    /// <summary>A section heading.</summary>
    private static Heading Section(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Accent,
        Margin = new Thickness(0, 0, 0, 6),
    };

    /// <summary>A header cell.</summary>
    private static TextBlock Th(string text) => Tb(text, 7.5f, Color.White, bold: true);

    /// <summary>One declaration: a label above its statement.</summary>
    private static VerticalStack Declaration(string label, string text)
    {
        VerticalStack stack = new() { Spacing = 2, Margin = new Thickness(0, 0, 16, 8) };
        stack.Add(Tb(label.ToUpperInvariant(), 6.5f, Mute, bold: true));
        stack.Add(Tb(text, 8.5f, Ink));
        return stack;
    }

    /// <summary>A conforming result's status, as a small chip.</summary>
    private static HorizontalStack Pass()
    {
        HorizontalStack fit = new() { WidthDistribution = WidthDistribution.Auto };
        fit.Add(new Container
        {
            BackgroundColor = ColorPalette.Emerald[50],
            CornerRadius = 3,
            Padding = new Thickness(5, 1.5f, 5, 1.5f),
            Child = Tb("Pass", 7.5f, ColorPalette.Emerald[800], bold: true),
        });
        return fit;
    }

    /// <summary>The band down the page's left edge, with the title and batch turned to read upwards.</summary>
    private static Container EdgeBand() => new()
    {
        BackgroundColor = Accent,
        Width = 24,
        Height = 841.89f,
        Padding = new Thickness(0, 241, 0, 0),
        Child = new Rotate
        {
            Angle = 270,
            Mode = RotationMode.Constrained,
            ContentWidth = 360,
            Child = new TextBlock("CERTIFICATE OF ANALYSIS  ·  BATCH 24-1187")
            {
                FontName = StandardFonts.HelveticaBold,
                FontSize = 8,
                LetterSpacing = 3,
                Color = Color.White,
                Alignment = TextAlignment.Center,
            },
        },
    };
}
