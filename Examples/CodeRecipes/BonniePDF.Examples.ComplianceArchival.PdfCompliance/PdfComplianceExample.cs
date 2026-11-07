using BonniePDF;

namespace BonniePDF.Examples.ComplianceArchival.PdfCompliance;

/// <summary>
/// A council tax letter that benefits from both standards: PDF/UA-1 so every resident can read it with a
/// screen reader, PDF/A-2a so the council can archive it unchanged.
/// </summary>
[System.ComponentModel.DisplayName("Archival & Accessible PDF")]
[System.ComponentModel.Category("Compliance & Archival")]
[System.ComponentModel.Description("A council tax letter that has to reach every resident and be kept for years: tagged for screen readers (PDF/UA-1), archived as PDF/A-2a, with the settings that make it so explained on the page.")]
public static class PdfComplianceExample
{
    /// <summary>The council's colour: a deep teal, used for the masthead and the table header.</summary>
    private static readonly Color Council = ColorPalette.Teal[800];

    /// <summary>The charge, by row: what it is for and the amount for the year.</summary>
    private static readonly (string Part, string Amount)[] Charges =
    [
        ("Sample Borough Council", "£1,642.18"),
        ("Adult social care", "£214.06"),
        ("County fire authority", "£98.40"),
        ("Police and crime commissioner", "£276.12"),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        // ── Footer: the sender on the left, a discreet standards badge on the right ──
        HorizontalStack footer = new() { WidthDistribution = WidthDistribution.Equal };
        footer.Add(new TextBlock("Sample Borough Council  ·  Revenues and Benefits") { FontSize = 8, Color = ColorPalette.Slate[500] });
        footer.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.End,
            Child = new Container
            {
                Border = new Border(0.75f),
                BorderColors = new BorderColors(ColorPalette.Teal[700]),
                CornerRadius = 4,
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock("PDF/A-2a  ·  PDF/UA-1") { FontName = StandardFonts.HelveticaBold, FontSize = 7, Color = ColorPalette.Teal[800] },
            },
        });

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Your council tax for 2027 to 2028",
                Author = "Sample Borough Council",
                Subject = "Council tax notice, archival (PDF/A-2a) and accessible (PDF/UA-1).",
                Keywords = "pdf/a-2a, pdf/ua-1, accessibility, archival, council tax, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10.5f).WithColor(ColorPalette.Slate[700]),
                FooterTemplate = PageTemplate.FromElements(@default: footer),
            },
        };

        // ── Masthead and address ──
        doc.Add(new Container
        {
            BackgroundColor = Council,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 20),
            Child = new TextBlock("Sample Borough Council") { FontName = StandardFonts.HelveticaBold, FontSize = 17, Color = Color.White },
        });
        HorizontalStack top = new() { WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 20) };
        top.Add(new TextBlock("Sample Resident\n12 Mill Lane\nSample Town\nST4 2QR"));
        top.Add(new TextBlock("5 November 2026\nAccount 7700 4821 93\nBand D") { Alignment = TextAlignment.Right, Color = ColorPalette.Slate[600] });
        doc.Add(top);

        doc.Add(new Heading("Your council tax for 2027 to 2028")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 8),
        });
        doc.Add(new TextBlock(
            "This letter sets out your council tax for the year from 1 April 2027. The total is £2,230.76, which we will collect from your bank account in ten monthly instalments: £223.13 on 1 April, then £223.07.")
        {
            Margin = new Thickness(0, 0, 0, 12),
        });

        // ── The charge: a real data table, with a header row screen readers announce ──
        Table table = new(3, 1)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            HeaderBackgroundColor = Council,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 14),
        };
        table.AddHeader(
            new TextBlock("Part of the charge") { FontName = StandardFonts.HelveticaBold },
            new TextBlock("Amount for the year") { FontName = StandardFonts.HelveticaBold, Alignment = TextAlignment.Right });
        foreach (var (part, amount) in Charges)
            table.AddRow(new TextBlock(part), new TextBlock(amount) { Alignment = TextAlignment.Right });
        table.AddFooter(
            new TextBlock("Total") { FontName = StandardFonts.HelveticaBold },
            new TextBlock("£2,230.76") { FontName = StandardFonts.HelveticaBold, Alignment = TextAlignment.Right });
        doc.Add(table);

        doc.Add(new Heading("What to do next")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 5),
        });
        UnorderedList steps = new()
        {
            FontSize = 10.5f,
            Color = ColorPalette.Slate[700],
            BulletColor = ColorPalette.Teal[700],
            ItemSpacing = 5,
            Margin = new Thickness(0, 0, 0, 20),
        };
        steps.AddItem("Nothing, if the details above are right: your first instalment is taken on 1 April.");
        steps.AddItem("Tell us within 21 days if you live alone, so we can apply the single person discount.");
        steps.AddItem("Call 0117 496 0123 or visit sample.example/counciltax if you need help paying.");
        doc.Add(steps);

        // ── How this letter was made: the settings, for the developer reading the example ──
        VerticalStack made = new() { Spacing = 4 };
        made.Add(new TextBlock("How this letter was made") { FontName = StandardFonts.HelveticaBold, Color = ColorPalette.Slate[900] });
        made.Add(new TextBlock(
            "It is set to the PDF/A-2a archival level, so it can be kept unchanged for years, and to PDF/UA-1, so its headings, list and table are tagged for screen readers; its language is British English. BonniePDF writes the metadata, colour profile, embedded fonts and tags. The author supplies the title, language, reading order and table headers, and validates the result.")
        {
            FontSize = 9,
            Color = ColorPalette.Slate[600],
        });
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            CornerRadius = 6,
            Padding = new Thickness(14, 12, 14, 12),
            Child = made,
        });

        return doc;
    }
}
