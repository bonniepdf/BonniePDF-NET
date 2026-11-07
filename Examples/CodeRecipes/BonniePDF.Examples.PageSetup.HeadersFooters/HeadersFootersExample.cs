using BonniePDF;
namespace BonniePDF.Examples.PageSetup.HeadersFooters;

/// <summary>A three-page report with a distinct first-page header, mirrored odd and even headers, and page numbers on the outer edge.</summary>
public static class HeadersFootersExample
{
    private const string Brand = "NORTHFIELD LOGISTICS";
    private const string Title = "Operations review  ·  Q3 2026";

    /// <summary>Heading and paragraphs - enough text to run across three pages.</summary>
    private static readonly (string Heading, string[] Paragraphs)[] Sections =
    [
        ("Summary",
        [
            "The third quarter was the busiest in our history. Parcel volumes rose 14% on the same quarter last year, driven by two new retail contracts in the North West and the first full quarter of the Leeds cross-dock. On-time delivery held at 97.2% despite the August heatwave and a week of rail disruption.",
            "Costs rose more slowly than volume. Fuel fell as a share of revenue for the third quarter running, helped by the electric vans now in service at three depots, and agency labour spend was 9% below plan after the spring recruitment drive."
        ]),
        ("Network performance",
        [
            "Leeds now handles a third of all northern trunking. Average dwell time at the cross-dock fell from 3 hours 40 minutes in July to 2 hours 55 minutes in September as the second sortation line came into use, and the site processed its millionth parcel on 19 September.",
            "First-attempt delivery success improved to 91.4%. The largest gain came from the new evening slots in Manchester, where customers can now choose a two-hour window up to 21:00. Missed deliveries cost us an estimated £410,000 in the quarter, down from £520,000.",
            "Damage claims were flat at 0.18% of parcels. Most claims still come from oversized items on shared routes; the trial of dedicated large-item rounds in Sheffield halved claims there and will extend to Bradford in November."
        ]),
        ("Fleet and depots",
        [
            "We took delivery of 42 electric vans in the quarter, bringing the electric fleet to 118 of 640 vehicles. Charging capacity remains the constraint: the Wakefield depot upgrade completes in December and adds 30 charge points, enough for the vans already on order.",
            "Two leases ended. The Hull satellite depot closed in August, with its rounds moved to Grimsby and Doncaster without loss of service, and the Preston site was renewed for ten years on improved terms.",
            "Vehicle availability averaged 95.8%, slightly below the 96% target. Tyre and brake wear on the electric vans is running higher than forecast, and we are reviewing maintenance intervals with the manufacturer."
        ]),
        ("People",
        [
            "Headcount rose to 2,140, including 96 drivers who completed the in-house licence programme. Voluntary turnover fell to 18% a year, the lowest since 2019, and the first cohort of twelve warehouse supervisors finished the leadership course in September.",
            "There were four reportable injuries, against seven in the same quarter last year. All four involved manual handling at depots that have not yet received the new roller conveyors; the remaining installations are scheduled before the peak season."
        ]),
        ("Outlook",
        [
            "Peak season planning is complete. We expect volumes 20% above last year from mid-November to Christmas, and have contracted 380 seasonal staff and 60 hired vans to cover it. The board is asked to approve the capital plan for the Doncaster expansion at its next meeting.",
            "Our priorities for the fourth quarter are unchanged: protect on-time delivery through peak, finish the Wakefield charging upgrade, and bring first-attempt success above 92% across the network."
        ]),
    ];

    private static readonly (string Measure, string Value, string Change)[] Kpis =
    [
        ("Parcels delivered", "18.6 m", "+14%"),
        ("On-time delivery", "97.2%", "+0.4 pts"),
        ("First-attempt success", "91.4%", "+1.8 pts"),
        ("Electric vans in service", "118", "+42"),
    ];

    /// <summary>Depot, parcels (k), on-time and first-attempt rates for the appendix table.</summary>
    private static readonly (string Depot, string Parcels, string OnTime, string FirstAttempt)[] Depots =
    [
        ("Leeds cross-dock", "6,180", "97.9%", "92.6%"),
        ("Manchester", "3,420", "97.4%", "93.1%"),
        ("Sheffield", "2,010", "96.8%", "90.2%"),
        ("Bradford", "1,640", "96.5%", "89.8%"),
        ("Wakefield", "1,380", "97.1%", "91.0%"),
        ("Preston", "1,290", "97.6%", "91.7%"),
        ("Doncaster", "1,170", "96.9%", "90.4%"),
        ("Grimsby", "910", "96.2%", "89.1%"),
        ("York", "600", "97.8%", "92.2%"),
    ];

    public static Document Generate()
    {
        Color teal = ColorPalette.Teal[700];
        HorizontalStack firstPage = new() { WidthDistribution = WidthDistribution.Equal };
        firstPage.Add(Small(Brand, TextAlignment.Left, bold: true, size: 11, color: teal));
        firstPage.Add(Small("Board pack  ·  14 October 2026", TextAlignment.Right));

        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Margin = 56,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10.5f).WithColor(ColorPalette.Slate[700]),
            // Page one carries the larger brand line; later pages alternate so the
            // brand and the page number always sit on the outer edge, as in a printed report.
            HeaderTemplate = PageTemplate.FromElements(
                firstPage: firstPage,
                odd: Running(Small(Title, TextAlignment.Left), Small(Brand, TextAlignment.Right, bold: true, color: teal), header: true),
                even: Running(Small(Brand, TextAlignment.Left, bold: true, color: teal), Small(Title, TextAlignment.Right), header: true)),
            FooterTemplate = PageTemplate.FromElements(
                odd: Running(Small("Confidential", TextAlignment.Left), Small("Page {{CurrentPage}} of {{TotalPages}}", TextAlignment.Right), header: false),
                even: Running(Small("Page {{CurrentPage}} of {{TotalPages}}", TextAlignment.Left), Small("Confidential", TextAlignment.Right), header: false))
        })
        { Metadata = new DocumentMetadata { Title = "Operations review Q3 2026" } };

        doc.Add(new Heading("Operations review, Q3 2026")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 26,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 8, 0, 6)
        });
        doc.Add(new TextBlock("July to September 2026. Prepared for the board by the operations team.")
        {
            FontSize = 12,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 18)
        });

        Table kpis = new(3f, 1.2f, 1.2f) { CellPadding = 7, HeaderBackgroundColor = teal, HeaderTextColor = Color.White };
        kpis.AddHeader(Cell("Measure", TextAlignment.Left, true), Cell("Q3 2026", TextAlignment.Right, true), Cell("vs Q3 2025", TextAlignment.Right, true));
        foreach (var (measure, value, change) in Kpis)
            kpis.AddRow(Cell(measure, TextAlignment.Left), Cell(value, TextAlignment.Right), Cell(change, TextAlignment.Right));
        doc.Add(new Container { Margin = new Thickness(0, 0, 0, 10), Child = kpis });

        foreach (var (heading, paragraphs) in Sections)
        {
            doc.Add(SectionHeading(heading, 14));
            foreach (string paragraph in paragraphs)
                doc.Add(new TextBlock(paragraph) { LineHeight = 1.5f, Margin = new Thickness(0, 0, 0, 8) });
        }

        // The appendix starts a page of its own, so the third page shows the odd-page header.
        doc.Add(new PageBreak());
        doc.Add(SectionHeading("Appendix: depot performance", 8));
        doc.Add(new TextBlock("Parcels handled and service levels by depot, July to September 2026.") { Margin = new Thickness(0, 0, 0, 10) });

        Table depots = new(3f, 1.2f, 1.2f, 1.2f) { CellPadding = 7, HeaderBackgroundColor = teal, HeaderTextColor = Color.White };
        depots.AddHeader(Cell("Depot", TextAlignment.Left, true), Cell("Parcels (k)", TextAlignment.Right, true), Cell("On time", TextAlignment.Right, true), Cell("First attempt", TextAlignment.Right, true));
        foreach (var (depot, parcels, onTime, firstAttempt) in Depots)
            depots.AddRow(Cell(depot, TextAlignment.Left), Cell(parcels, TextAlignment.Right), Cell(onTime, TextAlignment.Right), Cell(firstAttempt, TextAlignment.Right));
        doc.Add(depots);
        return doc;
    }

    private static Heading SectionHeading(string text, float top) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 15,
        Color = ColorPalette.Slate[900],
        Margin = new Thickness(0, top, 0, 6)
    };

    /// <summary>8.5pt header and footer text, grey unless a colour is given.</summary>
    private static TextBlock Small(string text, TextAlignment alignment, bool bold = false, float size = 8.5f, Color? color = null) => new(text)
    {
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        LetterSpacing = bold ? 1 : 0,
        Color = color ?? ColorPalette.Slate[500],
        Alignment = alignment
    };

    /// <summary>A two-part header line with a rule beneath it, or a footer line with a rule above it.</summary>
    private static Container Running(TextBlock left, TextBlock right, bool header)
    {
        HorizontalStack line = new() { WidthDistribution = WidthDistribution.Equal };
        line.Add(left);
        line.Add(right);
        return new Container
        {
            Border = header ? new Border(0, 0, 0, 0.75f) : new Border(0, 0.75f, 0, 0),
            BorderColors = new BorderColors(ColorPalette.Slate[300]),
            Padding = header ? new Thickness(0, 0, 0, 6) : new Thickness(0, 6, 0, 0),
            Child = line
        };
    }

    /// <summary>A 10pt table cell.</summary>
    private static TextBlock Cell(string text, TextAlignment alignment, bool bold = false) => new(text)
    {
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 10,
        Alignment = alignment
    };
}
