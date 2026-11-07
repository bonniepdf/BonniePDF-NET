using BonniePDF;

namespace BonniePDF.Examples.Hospitality;

/// <summary>
/// A banquet event order for a wedding reception: the day's timeline, a drawn room plan, dietary counts
/// by table, staffing with call times, and sign-off lines for the client, the event manager and the head chef.
/// </summary>
[System.ComponentModel.DisplayName("Banquet Event Order")]
[System.ComponentModel.Category("Hospitality")]
[System.ComponentModel.Description("A banquet event order for a wedding reception: the day’s timeline, a drawn room plan, dietary counts by table, staffing with call times, and sign-off lines for the client, the event manager and the head chef.")]
public static class BanquetEventOrderExample
{
    private static readonly Color Hotel = ColorPalette.Rose[900];
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Muted = ColorPalette.Stone[500];

    /// <summary>The day, in order: time, what happens, and the team responsible.</summary>
    private static readonly (string Time, string What, string Who)[] Timeline =
    [
        ("13:00", "Kitchen and porters on; room set begins", "Banqueting"),
        ("14:30", "Room set complete; walk-through with the client", "Event manager"),
        ("15:30", "Guests arrive; drinks reception on the terrace", "Bar"),
        ("16:30", "Call to dinner; doors to the Orangery open", "Event manager"),
        ("16:45", "Wedding breakfast served, three courses", "Kitchen, service"),
        ("18:15", "Speeches, then Champagne for the toast", "Service"),
        ("19:00", "Cake cutting; room turned for the evening", "Banqueting"),
        ("19:30", "Band starts; 40 evening guests arrive", "Event manager"),
        ("21:30", "Evening buffet", "Kitchen"),
        ("00:00", "Carriages", "Night porter"),
    ];

    /// <summary>Dietary requirements: what is needed, how many guests, and where they sit.</summary>
    private static readonly (string Need, int Guests, string Where)[] Dietary =
    [
        ("Standard menu", 74, "All tables"),
        ("Vegetarian", 6, "Tables 2, 5, 7"),
        ("Vegan", 3, "Tables 4, 8"),
        ("Gluten free", 2, "Top table, table 3"),
        ("Severe nut allergy", 1, "Table 4, seat 7"),
    ];

    /// <summary>Staffing: role, number on shift, and call time.</summary>
    private static readonly (string Role, int Count, string Call)[] Staff =
    [
        ("Event manager", 1, "13:00"),
        ("Chefs", 4, "13:00"),
        ("Porters", 2, "13:00"),
        ("Waiting staff", 9, "15:00"),
        ("Bar staff", 3, "15:00"),
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
                Margin = 36,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(ColorPalette.Stone[700]),
                FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Ardmore House Hotel  ·  Events office 0131 496 0745  ·  BEO 2611-14, issue 3")
                {
                    FontSize = 7.5f,
                    Color = Muted,
                    Alignment = TextAlignment.Center,
                }),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Banquet event order 2611-14",
                Author = "Ardmore House Hotel",
                Subject = "Wedding reception, 14 November 2026",
            },
        };

        // ── Masthead ──
        HorizontalStack masthead = new() { WidthDistribution = WidthDistribution.Equal };
        VerticalStack title = new() { Spacing = 3 };
        title.Add(new TextBlock("ARDMORE HOUSE  ·  EVENTS") { FontName = StandardFonts.HelveticaBold, FontSize = 8, LetterSpacing = 2, Color = Hotel });
        title.Add(new Heading("Banquet event order") { Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 20, Color = Ink, Margin = new Thickness(0) });
        title.Add(new TextBlock("Okafor and Lindqvist wedding reception") { FontSize = 10.5f, Color = ColorPalette.Stone[700] });
        masthead.Add(title);
        masthead.Add(new TextBlock("BEO 2611-14  ·  Issue 3\nIssued 5 November 2026\nFinal numbers due 9 November")
        {
            Alignment = TextAlignment.Right,
            Color = Muted,
        });
        doc.Add(new Container
        {
            Border = new Border(0, 0, 0, 1.5f),
            BorderColors = new BorderColors(Hotel),
            Padding = new Thickness(0, 0, 0, 8),
            Margin = new Thickness(0, 0, 0, 12),
            Child = masthead,
        });

        // ── The event at a glance ──
        HorizontalStack facts = new() { WidthDistribution = WidthDistribution.Equal, Spacing = 8, Margin = new Thickness(0, 0, 0, 14) };
        facts.Add(Fact("Date", "14 November 2026"));
        facts.Add(Fact("Room", "The Orangery"));
        facts.Add(Fact("Numbers", "86 guaranteed, 90 laid"));
        facts.Add(Fact("Client", "Sofia Lindqvist"));
        doc.Add(facts);

        // ── Timeline ──
        doc.Add(SectionHeading("Timeline"));
        Table timeline = new(0.6f, 4, 1.4f)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[100],
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 0, 0, 14),
        };
        timeline.AddHeader(Th("Time"), Th("What happens"), Th("Who"));
        foreach (var (time, what, who) in Timeline)
            timeline.AddRow(new TableRow { Cells = [Cell(time, Hotel, bold: true), Cell(what), Cell(who, Muted)] });
        doc.Add(timeline);

        // ── Room plan beside the dietary counts and staffing ──
        Table dietary = new(2, 0.8f, 2)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[100],
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 0, 0, 12),
        };
        dietary.AddHeader(Th("Requirement"), Th("Guests", TextAlignment.Right), Th("Where"));
        foreach (var (need, guests, where) in Dietary)
        {
            bool allergy = need.Contains("allergy", StringComparison.Ordinal);
            TableRow row = new()
            {
                Cells =
                [
                    Cell(need, allergy ? ColorPalette.Rose[800] : ColorPalette.Stone[800], allergy),
                    Cell(guests.ToString(System.Globalization.CultureInfo.InvariantCulture), ColorPalette.Stone[800], false, TextAlignment.Right),
                    Cell(where, allergy ? ColorPalette.Rose[800] : Muted),
                ],
            };
            if (allergy) row.BackgroundColor = ColorPalette.Rose[50];
            dietary.AddRow(row);
        }
        dietary.AddFooter(Th("Total"), Th(Dietary.Sum(row => row.Guests).ToString(System.Globalization.CultureInfo.InvariantCulture), TextAlignment.Right), Th(""));

        Table staffing = new(2, 0.8f, 2)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[100],
            HeaderTextColor = Ink,
        };
        staffing.AddHeader(Th("Role"), Th("Staff", TextAlignment.Right), Th("Call time"));
        foreach (var (role, count, call) in Staff)
        {
            staffing.AddRow(new TableRow
            {
                Cells =
                [
                    Cell(role),
                    Cell(count.ToString(System.Globalization.CultureInfo.InvariantCulture), ColorPalette.Stone[800], false, TextAlignment.Right),
                    Cell(call, Muted),
                ],
            });
        }

        VerticalStack plan = new() { Spacing = 4 };
        plan.Add(new CanvasElement(DrawRoomPlan)
        {
            DrawWidth = 0,
            DrawHeight = 206,
            AltText = "Room plan of the Orangery: the top table along the north wall, eight round tables of ten in two rows, and along the south wall the bar, the dance floor and the band stage. The doors are on the west wall.",
        });
        plan.Add(new TextBlock("Doors open from the terrace on the west wall. Cake table beside the top table.") { FontSize = 7.5f, Color = Muted });
        VerticalStack left = new();
        left.Add(SectionHeading("Room plan"));
        left.Add(plan);
        VerticalStack right = new();
        right.Add(SectionHeading("Dietary requirements"));
        right.Add(dietary);
        right.Add(SectionHeading("Staffing"));
        right.Add(staffing);
        HorizontalStack middle = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18, Margin = new Thickness(0, 0, 0, 14) };
        middle.Add(left, 1.1f);
        middle.Add(right, 1);
        doc.Add(middle);

        // ── Food and drink ──
        doc.Add(SectionHeading("Food and drink"));
        doc.Add(new TextBlock(
            "Wild mushroom soup; braised beef cheek, or spiced chickpea stew for vegan guests; sticky toffee pudding. Wines: house Rioja and Picpoul de Pinet, two glasses a head with dinner; Champagne for the toast. Evening buffet of bacon rolls and chips for 120.")
        {
            Margin = new Thickness(0, 0, 0, 18),
        });

        // ── Sign-offs ──
        HorizontalStack signOffs = new() { WidthDistribution = WidthDistribution.Equal, Spacing = 18 };
        foreach (string who in new[] { "Client: Sofia Lindqvist", "Event manager: Ross Baxter", "Head chef: Dana Fraser" })
        {
            VerticalStack line = new() { Spacing = 4 };
            line.Add(new Container
            {
                Height = 26,
                Border = new Border(0, 0, 0, 0.75f),
                BorderColors = new BorderColors(ColorPalette.Stone[400]),
            });
            line.Add(new TextBlock(who) { FontSize = 8, Color = Muted });
            signOffs.Add(line);
        }
        doc.Add(signOffs);
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>The Orangery from above: top table, eight rounds of ten, dance floor, band stage, bar and doors.</summary>
    private static void DrawRoomPlan(ICanvas canvas, float w, float h)
    {
        Color wall = ColorPalette.Stone[400];
        Color label = ColorPalette.Stone[600];
        canvas.FillRoundedRect(0, 0, w, h, 4, ColorPalette.Stone[50]);
        canvas.StrokeRoundedRect(0.5f, 0.5f, w - 1, h - 1, 4, wall, 1);
        // Doors: a gap in the west wall.
        canvas.FillRect(0, 10, 2, 28, ColorPalette.Stone[50]);
        Centred(canvas, "Doors", 18, 20, 6.5f, label);

        // Top table along the north wall.
        canvas.FillRoundedRect(w / 2 - 70, 12, 140, 14, 3, ColorPalette.Rose[900]);
        Centred(canvas, "Top table (10)", w / 2, 15.5f, 7, Color.White);

        // Eight rounds of ten in two rows, centred in quarters of the room.
        int table = 1;
        foreach (float y in new float[] { 58, 104 })
        {
            foreach (float quarter in new[] { 0.125f, 0.375f, 0.625f, 0.875f })
            {
                canvas.FillCircle(w * quarter, y, 17, ColorPalette.Stone[200]);
                Centred(canvas, (table++).ToString(System.Globalization.CultureInfo.InvariantCulture), w * quarter, y - 4.5f, 9, ColorPalette.Stone[800]);
            }
        }

        // Bar, dance floor and band stage along the south side, in sixteenths of the room.
        canvas.FillRoundedRect(w * 0.0625f, 140, w * 0.25f, 48, 3, ColorPalette.Stone[300]);
        Centred(canvas, "Bar", w * 0.1875f, 160, 7, label);
        canvas.FillRect(w * 0.375f, 136, w * 0.25f, 56, ColorPalette.Amber[100]);
        Centred(canvas, "Dance floor", w / 2, 160, 7, ColorPalette.Amber[900]);
        canvas.FillRoundedRect(w * 0.6875f, 140, w * 0.25f, 48, 3, ColorPalette.Stone[300]);
        Centred(canvas, "Band", w * 0.8125f, 160, 7, label);
    }

    /// <summary>A line of text centred on (x, y) in the room plan.</summary>
    private static void Centred(ICanvas canvas, string text, float x, float y, float size, Color color)
    {
        float width = canvas.MeasureText(text, size, StandardFonts.HelveticaBold).Width;
        canvas.DrawText(x - width / 2, y, text, color, size, StandardFonts.HelveticaBold, TextAlignment.Left);
    }

    /// <summary>A small labelled fact on a light panel.</summary>
    private static Container Fact(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(new TextBlock(label.ToUpperInvariant()) { FontName = StandardFonts.HelveticaBold, FontSize = 6.5f, LetterSpacing = 1, Color = Muted });
        stack.Add(new TextBlock(value) { FontSize = 9, Color = Ink });
        return new Container
        {
            BackgroundColor = ColorPalette.Stone[50],
            CornerRadius = 4,
            Padding = new Thickness(8, 7, 8, 7),
            Child = stack,
        };
    }

    /// <summary>A section heading.</summary>
    private static Heading SectionHeading(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Hotel,
        Margin = new Thickness(0, 0, 0, 5),
    };

    /// <summary>A header or footer cell: bold, aligned as its column.</summary>
    private static TextBlock Th(string text, TextAlignment alignment = TextAlignment.Left) =>
        new(text) { FontName = StandardFonts.HelveticaBold, FontSize = 8, Alignment = alignment };

    /// <summary>A body cell.</summary>
    private static TableCell Cell(string text, Color? color = null, bool bold = false, TextAlignment alignment = TextAlignment.Left) =>
        new()
        {
            Content = new TextBlock(text)
            {
                FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
                Color = color ?? ColorPalette.Stone[800],
                Alignment = alignment,
            },
        };
}
