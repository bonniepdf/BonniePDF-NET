using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.PropertyParticulars;

/// <summary>
/// The sales document an estate agency or property portal produces for every
/// instruction: the headline, the accommodation schedule with room dimensions in
/// both metric and imperial, the energy rating, tenure and council tax, and the
/// legal footer that stops the particulars becoming part of a contract.
///
/// The energy-rating graphic is drawn from layout primitives rather than dropped
/// in as an image - coloured bands, a pointer at the current rating and another
/// at the potential. That makes it a live piece of the document: change the
/// rating in the data and the graphic follows, no asset pipeline in between.
///
/// Photography is what a real brochure leads with. It is deliberately absent
/// here: an example carrying placeholder images reads as unfinished, and the
/// layout leaves the plate positions and their captions for real photographs to
/// drop into.
///
/// Sample property, agency, and figures throughout. Nothing here is a valuation
/// or property advice.
/// </summary>
[System.ComponentModel.DisplayName("Property particulars")]
[System.ComponentModel.Category("Property")]
[System.ComponentModel.Description("Estate agency particulars with an accommodation schedule in metric and imperial, an energy-rating graphic drawn from layout primitives rather than an image, and the misrepresentation notice. PDF/A-2a + PDF/UA-1.")]
public static class PropertyParticularsExample
{
    private const string Headline = "Three-bedroom semi-detached house";
    private const string Address = "27 Sample Gardens, Sample Town, ST5 3PN";
    private const decimal GuidePrice = 425000m;
    private const string Tenure = "Freehold";
    private const string CouncilTax = "Band D";
    private const int EpcCurrent = 68;
    private const int EpcPotential = 84;
    private const string YearBuilt = "c. 1935";
    private const string TotalArea = "118 m² / 1,270 ft²";
    private const string Agency = "Sample Estate Agency";
    private const string Reference = "SEA-2026-0442";
    private const string Viewing = "Strictly by appointment through the agency";

    private static readonly string[] Features =
    [
        "Three bedrooms, the principal bedroom with fitted wardrobes",
        "Two reception rooms, the rear opening onto the garden",
        "Extended kitchen and dining room fitted in 2024",
        "Family bathroom and a ground-floor cloakroom",
        "South-facing rear garden of approximately 14 metres",
        "Off-street parking for two vehicles",
        "Gas central heating, boiler installed 2023",
        "Within the catchment of Sample Primary School",
    ];

    private sealed record Room(string Name, string Floor, string Metric, string Imperial, string Note);

    private static readonly Room[] Rooms =
    [
        new("Entrance hall", "Ground", "4.20 × 1.85 m", "13'9\" × 6'1\"", "Original tiled floor, understairs storage"),
        new("Front reception", "Ground", "4.05 × 3.70 m", "13'3\" × 12'2\"", "Bay window to the front, open fireplace"),
        new("Rear reception", "Ground", "4.10 × 3.55 m", "13'5\" × 11'8\"", "French doors to the garden"),
        new("Kitchen and dining room", "Ground", "6.40 × 3.30 m", "21'0\" × 10'10\"", "Fitted 2024, integrated appliances, roof lantern"),
        new("Cloakroom", "Ground", "1.80 × 0.95 m", "5'11\" × 3'1\"", "WC and basin"),
        new("Principal bedroom", "First", "4.05 × 3.65 m", "13'3\" × 12'0\"", "Fitted wardrobes to one wall"),
        new("Bedroom two", "First", "3.95 × 3.40 m", "13'0\" × 11'2\"", "Overlooking the rear garden"),
        new("Bedroom three", "First", "2.85 × 2.30 m", "9'4\" × 7'7\"", "Currently arranged as a study"),
        new("Family bathroom", "First", "2.60 × 2.10 m", "8'6\" × 6'11\"", "Bath with shower over, heated towel rail"),
        new("Garden", "External", "14.0 × 8.5 m", "45'11\" × 27'11\"", "South facing, lawn with paved terrace"),
    ];

    /// <summary>
    /// The standard energy-rating bands, best first.
    ///
    /// Each band carries its own label colour as well as its fill. The
    /// recognisable rating colours run from dark green to bright red, and white
    /// text does not clear 4.5:1 on the lighter middle bands - so those take dark
    /// text instead. <c>Ink</c> is the colour to use when the band is named as
    /// text on a light panel, where the fill itself would be unreadable.
    /// </summary>
    private sealed record EpcBand(string Letter, int From, int To, Color Fill, Color Label, Color Ink);

    private static readonly EpcBand[] EpcBands =
    [
        new("A", 92, 100, ColorPalette.Emerald[800], Color.White, ColorPalette.Emerald[800]),
        new("B", 81, 91, ColorPalette.Emerald[700], Color.White, ColorPalette.Emerald[700]),
        new("C", 69, 80, ColorPalette.Lime[300], ColorPalette.Slate[900], ColorPalette.Lime[800]),
        new("D", 55, 68, ColorPalette.Amber[300], ColorPalette.Slate[900], ColorPalette.Amber[800]),
        new("E", 39, 54, ColorPalette.Amber[400], ColorPalette.Slate[900], ColorPalette.Amber[800]),
        new("F", 21, 38, ColorPalette.Orange[700], Color.White, ColorPalette.Orange[700]),
        new("G", 1, 20, ColorPalette.Rose[700], Color.White, ColorPalette.Rose[700]),
    ];

    private static EpcBand BandFor(int score)
        => EpcBands.FirstOrDefault(b => score >= b.From && score <= b.To) ?? EpcBands[^1];

    public static Document Generate()
    {
        EpcBand current = BandFor(EpcCurrent);
        EpcBand potential = BandFor(EpcPotential);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"{Agency} · {Reference}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"{Headline} — {Address}";
        doc.Metadata.Author = Agency;
        doc.Metadata.Subject =
            "Property particulars: headline, key features, accommodation schedule with metric and imperial dimensions, energy rating, tenure, and the misrepresentation notice (sample data).";
        doc.Metadata.Keywords =
            "property particulars, listing, estate agency, lettings, epc, accommodation schedule, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("PROPERTY PARTICULARS", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1(Headline));
        doc.Add(Cell(Address, size: 12f, color: Muted, marginBottom: 4f));
        doc.Add(Cell(
            $"Guide price {GuidePrice.ToString("C0", CultureInfo.GetCultureInfo("en-GB"))}",
            size: 18f,
            bold: true,
            color: Accent,
            marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Tile(Tenure, "Tenure"),
                Tile(CouncilTax, "Council tax"),
                Tile($"{current.Letter} ({EpcCurrent})", "Energy rating", current.Ink),
                Tile(TotalArea, "Approx. floor area"),
            ],
            spacing: 10f,
            marginBottom: 4f));

        doc.Add(H2("Key features"));
        foreach (string feature in Features)
        {
            doc.Add(Cell($"· {feature}", size: 9.5f, marginBottom: 3f));
        }

        doc.Add(H2("The property"));
        doc.Add(Cell(
            "A well-proportioned semi-detached house on a established residential road, extended to the rear to "
                + "create an open kitchen and dining room with a roof lantern. The two reception rooms retain their "
                + "original proportions, with a bay window to the front and doors from the rear room onto the "
                + "garden.",
            size: 9.5f,
            marginBottom: 6f));
        doc.Add(Cell(
            "Upstairs there are three bedrooms and a family bathroom. The south-facing garden extends to "
                + "approximately fourteen metres, with a paved terrace adjoining the house and lawn beyond. "
                + "Off-street parking is to the front for two vehicles.",
            size: 9.5f,
            marginBottom: 4f));

        doc.Add(H2("Accommodation"));
        doc.Add(Cell(
            "All dimensions are approximate maximums, taken at the widest point, and are given as a guide only.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        Table table = new(2.6f, 1f, 1.6f, 1.6f, 3.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(Th("Room"), Th("Floor"), Th("Metric"), Th("Imperial"), Th("Notes"));
        foreach (Room room in Rooms)
        {
            table.AddRow(
                Td(room.Name, bold: true),
                Td(room.Floor, color: Muted),
                Td(room.Metric),
                Td(room.Imperial, color: Muted),
                Td(room.Note));
        }

        doc.Add(table);

        // ── Energy rating ─────────────────────────────────────────────────────

        doc.Add(H2("Energy performance"));
        doc.Add(Cell(
            $"Current rating {current.Letter} ({EpcCurrent}). Potential after the recommended improvements: "
                + $"{potential.Letter} ({EpcPotential}).",
            size: 9.5f,
            marginBottom: 10f));
        doc.Add(EpcChart(EpcCurrent, EpcPotential));

        // ── Practicalities ────────────────────────────────────────────────────

        doc.Add(H2("Further information"));
        doc.Add(WeightedRow(
            [
                Field("Tenure", Tenure),
                Field("Council tax band", CouncilTax),
                Field("Approximate age", YearBuilt),
                Field("Reference", Reference),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
        [
            Field("Services", "Mains gas, electricity, water and drainage"),
            Field("Heating", "Gas central heating, boiler installed 2023"),
            Field("Parking", "Off-street for two vehicles"),
            Field("Viewing", Viewing),
        ]));

        doc.Add(Panel("Important notice",
        [
            Cell(
                "These particulars are prepared as a general guide and are not an offer or a contract, nor any "
                    + "part of one. Descriptions, dimensions, references to condition, and any necessary "
                    + "permissions for use and occupation are given in good faith and are believed to be correct, "
                    + "but any intending purchaser should not rely on them as statements of fact and must satisfy "
                    + "themselves by inspection or otherwise.",
                size: 8.5f,
                color: Muted,
                marginBottom: 3f),
            Cell(
                "No person in the employment of the agency has authority to make or give any representation or "
                    + "warranty in relation to this property. Services, appliances, and fittings have not been "
                    + "tested.",
                size: 8.5f,
                color: Muted),
        ]));

        return doc;
    }

    // ── The energy-rating graphic ─────────────────────────────────────────────

    /// <summary>
    /// The banded rating chart, drawn from layout primitives.
    ///
    /// Each band is a container whose width is proportional to its score range,
    /// so the arrangement is derived from the band table rather than
    /// hand-positioned. Changing a rating in the data moves the markers; there is
    /// no image to regenerate.
    /// </summary>
    private static VerticalStack EpcChart(int currentScore, int potentialScore)
    {
        VerticalStack stack = new() { Spacing = 3, Margin = new Thickness(0, 0, 0, 12) };

        stack.Add(Cell(
            "Energy efficiency rating — higher is better",
            size: 8.5f,
            bold: true,
            color: Muted,
            marginBottom: 4f));

        foreach (EpcBand epcBand in EpcBands)
        {
            bool isCurrent = currentScore >= epcBand.From && currentScore <= epcBand.To;
            bool isPotential = potentialScore >= epcBand.From && potentialScore <= epcBand.To;

            HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 6 };

            // The coloured band. Width tracks the band's position on the scale,
            // which is why A is the widest bar and G the narrowest.
            row.Add(
                new Container
                {
                    BackgroundColor = epcBand.Fill,
                    CornerRadius = 3,
                    Padding = new Thickness(5, 8),
                    Child = Cell(
                        $"{epcBand.Letter}   {epcBand.From}–{epcBand.To}",
                        size: 8.5f,
                        bold: true,
                        color: epcBand.Label),
                },
                Math.Max(1.2f, (epcBand.To - epcBand.From) / 10f));

            List<string> markers = [];
            if (isCurrent)
            {
                markers.Add($"Current {currentScore}");
            }

            if (isPotential)
            {
                markers.Add($"Potential {potentialScore}");
            }

            row.Add(
                Cell(
                    string.Join("   ·   ", markers),
                    size: 8.5f,
                    bold: markers.Count > 0,
                    color: markers.Count > 0 ? Ink : Muted),
                3);

            // Pad the right so short bands do not stretch to the full width.
            row.Add(new Container(), Math.Max(0.2f, (100 - (epcBand.To - epcBand.From)) / 22f));

            stack.Add(row);
        }

        return stack;
    }
}
