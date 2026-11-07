using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.FieldServiceProperty.PropertyParticulars;

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
/// The floor plans are drawn the same way: an SVG generated from the room data,
/// so a changed room size redraws the plan. Photography is what a real brochure
/// leads with; it is deliberately absent here, because an example carrying
/// placeholder images reads as unfinished.
///
/// Sample property, agency, and figures throughout. Nothing here is a valuation
/// or property advice.
/// </summary>
[System.ComponentModel.DisplayName("Property particulars")]
[System.ComponentModel.Category("Field Service & Property")]
[System.ComponentModel.Description(
    "Estate agency particulars with an accommodation schedule in metric and imperial, an energy-rating graphic drawn from layout primitives rather than an image, and the misrepresentation notice. PDF/A-2a + PDF/UA-1.")]
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
        new("Kitchen and dining room", "Ground", "6.40 × 3.30 m", "21'0\" × 10'10\"",
            "Fitted 2024, integrated appliances, roof lantern"),
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

    /// <summary>Label, x, y, width and depth in metres, front of the house at the top. Not to scale in the legal sense.</summary>
    private static readonly (string Label, double X, double Y, double Width, double Depth)[] GroundFloor =
    [
        ("Front reception", 0, 0, 4.05, 3.7),
        ("Hall", 4.05, 0, 2.35, 4.2),
        ("Rear reception", 0, 3.7, 4.1, 3.55),
        ("WC", 4.1, 4.2, 2.3, 1.2),
        ("Store", 4.1, 5.4, 2.3, 1.85),
        ("Kitchen and dining", 0, 7.25, 6.4, 3.3),
    ];

    private static readonly (string Label, double X, double Y, double Width, double Depth)[] FirstFloor =
    [
        ("Principal bedroom", 0, 0, 4.05, 3.65),
        ("Landing", 4.05, 0, 2.35, 2.4),
        ("Bathroom", 4.05, 2.4, 2.35, 2.1),
        ("Bedroom two", 0, 3.65, 4.05, 3.6),
        ("Bedroom three", 4.05, 4.5, 2.35, 2.75),
    ];

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

    private static readonly (string Path, string AltText)[] Gallery =
    [
        ("images/property/kitchen.jpg",
            "Extended kitchen and dining room with shaker units, oak worktops, and roof lantern."),
        ("images/property/living-room.jpg", "Front reception room with bay window and open fireplace."),
        ("images/property/garden.jpg", "South-facing rear garden with paved terrace and lawn."),
    ];

    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/property/hero.jpg",
        "images/property/kitchen.jpg",
        "images/property/living-room.jpg",
        "images/property/garden.jpg",
    ];

    private static EpcBand BandFor(int score)
        => EpcBands.FirstOrDefault(b => score >= b.From && score <= b.To) ?? EpcBands[^1];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document using the supplied asset dictionary.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "MA0051:Method is too long", Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

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
            marginBottom: 12f));

        doc.Add(PhotoFrame(
            Asset(assets, "images/property/hero.jpg"),
            150f,
            6f,
            "Front elevation of 14 Meadowcroft Avenue, a 1930s semi-detached house with a block-paved drive.",
            8f));

        HorizontalStack gallery = new()
        {
            Spacing = 8,
            WidthDistribution = WidthDistribution.Equal,
            Margin = new Thickness(0, 0, 0, 12),
        };
        foreach ((string path, string altText) in Gallery)
        {
            gallery.Add(PhotoFrame(Asset(assets, path), 68f, 4f, altText));
        }

        doc.Add(gallery);

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

        // ── Floor plans, drawn as SVG from the room data ──────────────────────
        doc.Add(H2("Floor plans"));
        doc.Add(WeightedRow(
            [
                PlanColumn("Ground floor", GroundFloor,
                    "Ground floor plan: front reception and hall at the front, rear reception, WC and store behind, and the kitchen and dining room across the back."),
                PlanColumn("First floor", FirstFloor,
                    "First floor plan: principal bedroom and landing at the front, bathroom off the landing, and bedrooms two and three at the back."),
            ],
            spacing: 24f, marginBottom: 4f));
        doc.Add(Cell("Floor plans are for illustration only and are not to scale.", size: 8.5f, color: Muted, marginBottom: 4f));

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

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Cyan[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with trade-document defaults.</summary>
    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left, float marginTop = 0f, float marginBottom = 0f) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
        Margin = new Thickness(0, marginTop, 0, marginBottom),
    };

    /// <summary>Table header cell - white on the accent band.</summary>
    /// <summary>A labelled floor plan.</summary>
    private static VerticalStack PlanColumn(string title, (string Label, double X, double Y, double Width, double Depth)[] rooms, string altText)
    {
        VerticalStack column = new() { Spacing = 4 };
        column.Add(Cell(title, size: 9.5f, bold: true, color: Heading));
        column.Add(new Svg { SvgData = FloorPlan(rooms), Width = 200, AltText = altText });
        return column;
    }

    /// <summary>A coordinate in the plan's SVG units, rounded to a tenth so both languages write the same digits.</summary>
    private static string N(double value) => (Math.Round(value * 10, MidpointRounding.AwayFromZero) / 10).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// An SVG floor plan: one outlined rectangle per room, labelled with its name and size,
    /// inside a heavier outer wall. 24 SVG units to the metre.
    /// </summary>
    private static string FloorPlan((string Label, double X, double Y, double Width, double Depth)[] rooms)
    {
        const double scale = 24;
        const double pad = 4;
        double width = rooms.Max(r => r.X + r.Width);
        double depth = rooms.Max(r => r.Y + r.Depth);
        var parts = rooms.Select(r =>
        {
            double cx = pad + (r.X + r.Width / 2) * scale;
            double cy = pad + (r.Y + r.Depth / 2) * scale;
            string size = r.Width >= 3
                ? $"{r.Width.ToString("0.00", CultureInfo.InvariantCulture)} × {r.Depth.ToString("0.00", CultureInfo.InvariantCulture)} m"
                : "";
            string labelSize = r.Width >= 3 ? "7.5" : "6.5";
            return $"<rect x=\"{N(pad + r.X * scale)}\" y=\"{N(pad + r.Y * scale)}\" width=\"{N(r.Width * scale)}\" height=\"{N(r.Depth * scale)}\" fill=\"#ffffff\" stroke=\"#64748b\" stroke-width=\"1\"/>"
                + $"<text x=\"{N(cx)}\" y=\"{N(size.Length > 0 ? cy - 1 : cy + 2.5)}\" font-family=\"Helvetica\" font-size=\"{labelSize}\" font-weight=\"bold\" fill=\"#1e293b\" text-anchor=\"middle\">{r.Label}</text>"
                + (size.Length > 0 ? $"<text x=\"{N(cx)}\" y=\"{N(cy + 8)}\" font-family=\"Helvetica\" font-size=\"6.5\" fill=\"#475569\" text-anchor=\"middle\">{size}</text>" : "");
        });
        double outerW = width * scale;
        double outerD = depth * scale;
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {N(outerW + 2 * pad)} {N(outerD + 2 * pad)}\">"
            + $"<rect x=\"{N(pad)}\" y=\"{N(pad)}\" width=\"{N(outerW)}\" height=\"{N(outerD)}\" fill=\"#f8fafc\"/>"
            + string.Concat(parts)
            + $"<rect x=\"{N(pad)}\" y=\"{N(pad)}\" width=\"{N(outerW)}\" height=\"{N(outerD)}\" fill=\"none\" stroke=\"#1e293b\" stroke-width=\"3\"/>"
            + "</svg>";
    }

    private static TextBlock Th(string text, TextAlignment align = TextAlignment.Left) => Cell(text, size: 8.5f, bold: true, color: Color.White, align: align);

    /// <summary>Table body cell.</summary>
    private static TextBlock Td(string text, float size = 8.5f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left) => Cell(text, size: size, bold: bold, color: color, align: align);

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        Color = Heading,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H2(string text, Color? color = null) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = color ?? Accent,
        Margin = new Thickness(0, 16, 0, 8),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    private static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>One label/value pair for the reference grid.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        stack.Add(Cell(value, size: 9.5f));
        return stack;
    }

    /// <summary>A row of evenly weighted elements.</summary>
    private static HorizontalStack WeightedRow(IEnumerable<IElement> items, float spacing = 14f, float marginTop = 0f, float marginBottom = 14f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = spacing,
            Margin = new Thickness(0, marginTop, 0, marginBottom),
        };
        foreach (IElement item in items)
        {
            row.Add(item, 1);
        }

        return row;
    }

    /// <summary>A bordered panel with a heading - terms, notes, declarations.</summary>
    private static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack body = new()
        {
            Spacing = 4
        };
        body.Add(Cell(title, size: 9.5f, bold: true, color: accent ?? Accent, marginBottom: 4f));
        foreach (IElement child in children)
        {
            body.Add(child);
        }

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 12),
            Child = body,
        };
    }

    /// <summary>A summary tile.</summary>
    private static Container Tile(string value, string label, Color? accent = null)
    {
        VerticalStack body = new();
        body.Add(Cell(value, size: 14f, bold: true, color: accent ?? Accent, marginBottom: 2f));
        body.Add(Cell(label, size: 8f, color: Muted));
        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(11, 10),
            Child = body,
        };
    }

    /// <summary>The running header / footer band.</summary>
    private static HorizontalStack Band(string left, string right)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted
        };
        row.Add(Cell(left, size: 7.5f, color: Muted), 2.5f);
        row.Add(Cell(right, size: 7.5f, color: Muted, align: TextAlignment.Right), 2);
        return row;
    }

    private static Container PhotoFrame(byte[] imageData, float height, float cornerRadius, string altText, float marginBottom = 0f) => new()
    {
        Height = height,
        CornerRadius = cornerRadius,
        ClipContent = true,
        Margin = new Thickness(0, 0, 0, marginBottom),
        Child = new Image
        {
            ImageData = imageData,
            Height = height,
            FitMode = ImageFitMode.Cover,
            AltText = altText,
        },
    };

    private static byte[] Asset(IReadOnlyDictionary<string, byte[]> assets, string path)
        => assets.TryGetValue(path, out byte[]? bytes)
            ? bytes
            : throw new InvalidOperationException($"Missing required asset: {path}");

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
            {
                assetsDir = candidate;
                break;
            }

            dir = Path.GetDirectoryName(dir);
        }

        Dictionary<string, byte[]> assets = new(StringComparer.Ordinal);
        foreach (string path in RequiredAssets)
        {
            string fullPath = Path.Combine(assetsDir, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                assets[path] = File.ReadAllBytes(fullPath);
            }
        }

        return assets;
    }
}
