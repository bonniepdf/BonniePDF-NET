using System.Globalization;
using BonniePDF;
using BonniePDF.Drawing;

namespace BonniePDF.Examples.Sustainability;

/// <summary>
/// An energy performance certificate for a house: the A to G rating scale drawn with the current and
/// potential ratings marked, each part of the building rated, the recommended improvements with their
/// cost and saving, and the assessor's details.
/// </summary>
[System.ComponentModel.DisplayName("Energy Performance Certificate")]
[System.ComponentModel.Category("Sustainability")]
[System.ComponentModel.Description("An energy performance certificate for a house: the A to G rating scale drawn with the current and potential ratings marked, each part of the building rated, the recommended improvements with their cost and saving, and the assessor’s details.")]
public static class EnergyPerformanceCertificateExample
{
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Body = ColorPalette.Stone[700];
    private static readonly Color Mute = ColorPalette.Stone[600];
    private static readonly Color Green = ColorPalette.Green[800];

    /// <summary>The rating bands, best first: letter, lowest score in the band, label, and the band's colour.</summary>
    private static readonly (string Letter, int Low, string Range, Color Color)[] Bands =
    [
        ("A", 92, "92+", ColorPalette.Green[700]),
        ("B", 81, "81 to 91", ColorPalette.Green[500]),
        ("C", 69, "69 to 80", ColorPalette.Lime[500]),
        ("D", 55, "55 to 68", ColorPalette.Yellow[400]),
        ("E", 39, "39 to 54", ColorPalette.Amber[500]),
        ("F", 21, "21 to 38", ColorPalette.Orange[600]),
        ("G", 1, "1 to 20", ColorPalette.Red[600]),
    ];

    private const int Current = 64;
    private const int Potential = 83;

    /// <summary>Each part of the building: feature, what it is, and how it rates.</summary>
    private static readonly (string Feature, string Description, string Rating)[] Features =
    [
        ("Walls", "Cavity wall, filled cavity", "Good"),
        ("Roof", "Pitched, 270 mm loft insulation", "Very good"),
        ("Windows", "Fully double glazed", "Good"),
        ("Main heating", "Gas boiler and radiators", "Good"),
        ("Heating controls", "Programmer, room thermostat and radiator valves", "Good"),
        ("Hot water", "From the main heating system", "Good"),
        ("Lighting", "Low-energy lighting in all fixed outlets", "Very good"),
        ("Floor", "Suspended timber, no insulation", "Poor"),
    ];

    /// <summary>Recommended improvements, in order: measure, typical cost, typical yearly saving, and the rating after it.</summary>
    private static readonly (string Measure, string Cost, string Saving, int After)[] Steps =
    [
        ("Floor insulation (suspended floor)", "£800 to £1,200", "£75", 67),
        ("Solar water heating", "£4,000 to £6,000", "£45", 69),
        ("Solar panels, 3.5 kWp", "£5,000 to £8,000", "£540", 83),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 40,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Body),
                FooterTemplate = PageTemplate.FromElements(@default: new TextBlock(
                    "Sample certificate for illustration  ·  Certificate number 0000-0000-0000-0000-0000  ·  Valid until 4 November 2036")
                {
                    FontSize = 7,
                    Color = Mute,
                }),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Energy performance certificate: 14 Orchard Row",
                Author = "Sample Energy Assessors Ltd",
                Subject = "Energy performance certificate for a semi-detached house",
            },
        };

        // ── Title and the property ──
        HorizontalStack top = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 16, Margin = new Thickness(0, 0, 0, 14) };
        VerticalStack title = new() { Spacing = 3 };
        title.Add(Tb("ENERGY PERFORMANCE CERTIFICATE", 7.5f, Green, bold: true));
        title.Add(new Heading("14 Orchard Row, Sample Town ST4 2QR")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = Ink,
            Margin = new Thickness(0),
        });
        title.Add(Tb("Semi-detached house  ·  Total floor area 96 m²  ·  Assessed 5 November 2026", 9, Mute));
        top.Add(title, 1);
        VerticalStack rating = new() { Spacing = 1 };
        rating.Add(Tb("RATING", 6.5f, Ink, bold: true));
        rating.Add(Tb($"{Letter(Current)} {Current}", 20, Ink, bold: true));
        top.Add(new Container { BackgroundColor = RatingColor(Current), CornerRadius = 6, Padding = new Thickness(14, 8, 14, 8), Child = rating });
        doc.Add(top);

        // ── The rating scale ──
        doc.Add(Section("Energy rating"));
        HorizontalStack scale = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18, Margin = new Thickness(0, 0, 0, 14) };
        scale.Add(new CanvasElement(DrawScale)
        {
            DrawWidth = 0,
            DrawHeight = 248,
            AltText = $"Energy rating scale from A, most efficient, to G. This property is rated {Letter(Current)} with a score of {Current}, and could reach {Letter(Potential)} with a score of {Potential}.",
        }, 1.5f);
        VerticalStack summary = new() { Spacing = 6 };
        summary.Add(Tb($"This property's current rating is {Letter(Current)}. It has the potential to be {Letter(Potential)}.", 10, Ink, bold: true));
        summary.Add(Tb("The rating is based on the running costs of heating, hot water and lighting under standard occupancy. The average rating for a house in England and Wales is D (60).", 8.5f, Body));
        summary.Add(Figures());
        scale.Add(summary, 1);
        doc.Add(scale);

        // ── How each part of the building rates ──
        doc.Add(Section("Breakdown of the energy performance"));
        Table breakdown = new(1.1f, 3, 1)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[100],
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 0, 0, 14),
        };
        breakdown.AddHeader(Th("Feature"), Th("Description"), Th("Rating"));
        foreach (var (feature, description, featureRating) in Features)
        {
            breakdown.AddRow(new TableRow
            {
                Cells =
                [
                    new TableCell { Content = Tb(feature, 8.5f, Ink, bold: true) },
                    new TableCell { Content = Tb(description, 8.5f, Body) },
                    new TableCell { Content = Tb(featureRating, 8.5f, string.Equals(featureRating, "Poor", StringComparison.Ordinal) ? ColorPalette.Rose[700] : Green, bold: true) },
                ],
            });
        }
        doc.Add(breakdown);

        // ── Recommended improvements ──
        doc.Add(Section("How to improve this property"));
        Table steps = new(0.4f, 2.6f, 1.4f, 1.1f, 1.1f)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = Green,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 14),
        };
        steps.AddHeader(Th("Step", Color.White), Th("Improvement", Color.White), Th("Typical cost", Color.White), Th("Yearly saving", Color.White), Th("Rating after", Color.White));
        for (int i = 0; i < Steps.Length; i++)
        {
            var (measure, cost, saving, after) = Steps[i];
            steps.AddRow(new TableRow
            {
                Cells =
                [
                    new TableCell { Content = Tb((i + 1).ToString(CultureInfo.InvariantCulture), 8.5f, Ink, bold: true) },
                    new TableCell { Content = Tb(measure, 8.5f, Ink) },
                    new TableCell { Content = Tb(cost, 8.5f, Body) },
                    new TableCell { Content = Tb(saving, 8.5f, Body) },
                    new TableCell { Content = RatingChip(after) },
                ],
            });
        }
        doc.Add(steps);

        // ── The assessment ──
        Grid assessor = new(1, 1, 1) { CellPadding = 0 };
        assessor.AddRow(
            Detail("Assessor", "R. Hughes, Sample Energy Assessors Ltd"),
            Detail("Accreditation", "SAMPLE-123456"),
            Detail("Contact", "assessor@energy.example"));
        doc.Add(Tb("This certificate shows how energy efficient the property is and how it could be improved. Costs and savings are typical figures from the assessment and will vary with how the home is used. You can use the certificate when you sell or let the property.", 8.5f, Body, bottom: 12));
        doc.Add(new Container
        {
            Border = new Border(0, 0.5f, 0, 0),
            BorderColors = new BorderColors(ColorPalette.Stone[300]),
            Padding = new Thickness(0, 8, 0, 0),
            Child = assessor,
        });
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Text in Helvetica or Helvetica Bold.</summary>
    private static TextBlock Tb(string text, float size, Color color, bool bold = false, float bottom = 0)
    {
        TextBlock block = new(text)
        {
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (bottom > 0) block.Margin = new Thickness(0, 0, 0, bottom);
        return block;
    }

    /// <summary>A section heading.</summary>
    private static Heading Section(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Green,
        Margin = new Thickness(0, 0, 0, 6),
    };

    /// <summary>A table header cell.</summary>
    private static TextBlock Th(string text, Color? color = null) => Tb(text, 7.5f, color ?? Ink, bold: true);

    /// <summary>The band letter for a score.</summary>
    private static string Letter(int score) => Bands.First(band => score >= band.Low).Letter;

    /// <summary>The band colour for a score.</summary>
    private static Color RatingColor(int score) => Bands.First(band => score >= band.Low).Color;

    /// <summary>A rating as a small chip in its band's colour.</summary>
    private static HorizontalStack RatingChip(int score)
    {
        HorizontalStack fit = new() { WidthDistribution = WidthDistribution.Auto };
        fit.Add(new Container
        {
            BackgroundColor = RatingColor(score),
            CornerRadius = 3,
            Padding = new Thickness(5, 1.5f, 5, 1.5f),
            Child = Tb($"{Letter(score)} {score}", 8, Ink, bold: true),
        });
        return fit;
    }

    /// <summary>The estimated yearly energy use, emissions and costs.</summary>
    private static Grid Figures()
    {
        Grid grid = new(1.4f, 1) { CellPadding = 3, Border = new Border(0, 0, 0, 0.5f), BorderColors = new BorderColors(ColorPalette.Stone[200]) };
        foreach (var (label, value) in new[]
        {
            ("Energy use", "12,400 kWh a year"),
            ("Carbon dioxide", "2.3 tonnes a year"),
            ("Heating cost", "£890 a year"),
            ("Hot water cost", "£165 a year"),
            ("Lighting cost", "£72 a year"),
        })
        {
            grid.AddRow(Tb(label, 8.5f, Mute), Tb(value, 8.5f, Ink, bold: true));
        }
        return grid;
    }

    /// <summary>A labelled detail of the assessment.</summary>
    private static VerticalStack Detail(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(Tb(label.ToUpperInvariant(), 6.5f, Mute, bold: true));
        stack.Add(Tb(value, 8.5f, Ink));
        return stack;
    }

    /// <summary>The A to G ladder, with arrows marking the current and potential ratings in their own columns.</summary>
    private static void DrawScale(ICanvas canvas, float width, float height)
    {
        const float rowHeight = 28;
        const float gap = 5;
        const float column = 54;
        float ladder = width - column * 2 - 8;
        Color label = ColorPalette.Stone[600];
        canvas.DrawText(ladder + 8, 0, "Current", label, 7.5f, StandardFonts.HelveticaBold, TextAlignment.Left);
        canvas.DrawText(ladder + 8 + column, 0, "Potential", label, 7.5f, StandardFonts.HelveticaBold, TextAlignment.Left);
        for (int i = 0; i < Bands.Length; i++)
        {
            var (band, _, range, color) = Bands[i];
            float top = 14 + i * (rowHeight + gap);
            float barWidth = ladder * (0.42f + i * 0.09f);
            canvas.FillRect(0, top, barWidth, rowHeight, color);
            Color ink = i is >= 1 and <= 4 ? ColorPalette.Stone[900] : Color.White;
            canvas.DrawText(6, top + 9, range, ink, 7.5f, StandardFonts.Helvetica, TextAlignment.Left);
            float w = canvas.MeasureText(band, 14, StandardFonts.HelveticaBold).Width;
            canvas.DrawText(barWidth - w - 8, top + 6, band, ink, 14, StandardFonts.HelveticaBold, TextAlignment.Left);
        }
        Arrow(canvas, ladder + 8, Current, rowHeight, gap, column - 6);
        Arrow(canvas, ladder + 8 + column, Potential, rowHeight, gap, column - 6);
    }

    /// <summary>An arrow pointing left at a score's band, carrying the score.</summary>
    private static void Arrow(ICanvas canvas, float x, int score, float rowHeight, float gap, float width)
    {
        int index = Array.FindIndex(Bands, band => score >= band.Low);
        float top = 14 + index * (rowHeight + gap);
        canvas.FillPath(
        [
            new Point(x, top + rowHeight / 2),
            new Point(x + 10, top),
            new Point(x + width, top),
            new Point(x + width, top + rowHeight),
            new Point(x + 10, top + rowHeight),
        ],
            Bands[index].Color);
        string text = $"{score} {Bands[index].Letter}";
        float w = canvas.MeasureText(text, 9, StandardFonts.HelveticaBold).Width;
        canvas.DrawText(x + 10 + (width - 10 - w) / 2, top + 9, text, ColorPalette.Stone[900], 9, StandardFonts.HelveticaBold, TextAlignment.Left);
    }
}
