using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Templates.AnnualReport;

/// <summary>
/// An A4 portrait annual-report template built around the page-slot features:
/// a full-bleed front cover painted by the page-background slot, then inner
/// pages whose background slot paints blue header/footer bands while the
/// header/footer slots carry the BonniePDF mark, generation time, and page
/// numbers around the flowing report content.
/// </summary>
[System.ComponentModel.DisplayName("Annual - Classic")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description(
    "A4 portrait annual report with a full-bleed cover, page backgrounds, and blue header/footer bands carrying the BonniePDF mark, generation time, and page numbers.")]
public static class AnnualReportExample
{
    // ── Brand colours ───────────────────────────────────────────────────────
    // Three brand colours drive the whole template - the cover accents, the
    // tri-colour rule under the title, the header/footer hairlines, and the KPI
    // cards all draw from this trio (plus tints derived from the primary).
    private static readonly Color Primary = ColorPalette.Blue[800]; // deep corporate blue
    private static readonly Color Secondary = ColorPalette.Teal[500]; // teal accent
    private static readonly Color Tertiary = ColorPalette.Amber[400]; // warm gold accent

    // Darker steps of Secondary/Tertiary for the KPI card value, which paints 24pt
    // bold text on the Blue-50 card panel: the accent's own shade only clears 3:1
    // large-text at Primary's depth (Secondary falls to 2.23:1, Tertiary to
    // 1.58:1). The swatches/rule/border above keep the lighter brand trio.
    private static readonly Color SecondaryText = ColorPalette.Teal[600];
    private static readonly Color TertiaryText = ColorPalette.Amber[700];

    private static readonly Color CoverBg = ColorPalette.Blue[900]; // deepest primary tint - cover field
    private static readonly Color BandBg = ColorPalette.Blue[100]; // lightest primary tint - header/footer bands
    private static readonly Color Ink = ColorPalette.Slate[600]; // body copy
    private static readonly Color HeadingColor = ColorPalette.Blue[800]; // section headings

    // A4 portrait. The generous margin doubles as the height of the full-bleed
    // header/footer bands, leaving the band text comfortably clear of the paper
    // edge and a hairline of breathing room between the bands and the content.
    private const float Margin = 72f;

    private const string HeroImage = "images/sean-pollock-PhYq704ffdA-unsplash.jpg";

    private const string Company = "Sample Company";
    private const int Year = 2025;
    private const string ReportTitle = "Annual Report 2025";

    // Captured once, when the document is generated - surfaced in the footer.
    private static readonly string Generated =
        "Generated " + DateTime.Now.ToString("d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = Margin,
            Conformance = PdfAConformance.PdfA2a, // the accessible archival level - requires tagging
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",

            // The cover gets a full-bleed navy field; every inner page gets the
            // blue header/footer bands. The slot ignores margins, so the bands
            // bleed to the paper edge with content overlaid on top.
            PageBackground = pageNumber => pageNumber == 1 ? CoverBackground() : PageBands(),

            // Suppressed on the cover; on every inner page they sit centred
            // inside the top/bottom margin bands painted by the background slot.
            HeaderTemplate = pageNumber => pageNumber == 1 ? null : HeaderRow(pageNumber),
            FooterTemplate = pageNumber => pageNumber == 1 ? null : FooterRow(pageNumber),
        };

        Document doc = new(settings);
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(Ink);

        doc.Metadata.Title = $"{Company} - {ReportTitle}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"{Company} {ReportTitle} - front cover, header/footer slots, and page backgrounds";

        // ══════════════════════════════════════════════════════════════════
        //  Page 1 - front cover (overlaid on the deep-blue background slot)
        // ══════════════════════════════════════════════════════════════════
        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.TryGetValue(HeroImage, out byte[]? heroData) || heroData.Length == 0)
            throw new InvalidOperationException($"Missing required asset: {HeroImage}");

        doc.Add(new Image
        {
            ImageData = heroData,
            Width = PdfSize.A4.Width - Margin * 2,
            Height = 220,
            FitMode = ImageFitMode.Cover,
            IsDecorative = true,
            Margin = new Thickness(0, 24, 0, 24),
        });
        HorizontalStack mark = BrandMark();
        mark.Margin = new Thickness(0, 0, 0, 26);
        doc.Add(mark);
        doc.Add(CoverLabel("ANNUAL REPORT"));
        doc.Add(CoverTitle(Company));
        doc.Add(TriColourRule());
        doc.Add(CoverSubtitle($"Fiscal Year {Year}"));
        doc.Add(new Spacer(40));
        doc.Add(CoverCredit("Prepared with BonniePDF"));
        doc.Add(CoverCredit(Generated));

        // ══════════════════════════════════════════════════════════════════
        //  Page 2 - letter to shareholders + performance highlights
        // ══════════════════════════════════════════════════════════════════
        doc.AddPage();
        Heading shareholderHeading = SectionHeading("Letter to Shareholders");
        shareholderHeading.Margin = new Thickness(0, 10, 0, 12);
        doc.Add(shareholderHeading);
        doc.Add(Body(
            "This past year tested our resolve and rewarded our discipline. Against a " +
            $"backdrop of shifting markets, {Company} delivered record revenue, expanded " +
            "into three new regions, and strengthened the balance sheet that underpins " +
            "everything we build. None of this would have been possible without the trust " +
            "you place in us as shareholders."));
        doc.Add(Body(
            "We entered the year with a simple promise: grow responsibly and invest where " +
            "it matters. Our teams answered that call. Operating margin improved for the " +
            "fourth consecutive year, customer retention reached an all-time high, and our " +
            "flagship platform now serves more than two million daily users."));
        Heading performanceHeading = SectionHeading("Performance Highlights");
        performanceHeading.Margin = new Thickness(0, 20, 0, 12);
        doc.Add(performanceHeading);
        doc.Add(KpiRow());

        // ══════════════════════════════════════════════════════════════════
        //  Page 3 - operational review (flows naturally, repeating the bands)
        // ══════════════════════════════════════════════════════════════════
        doc.AddPage();
        Heading operationalHeading = SectionHeading("Operational Review");
        operationalHeading.Margin = new Thickness(0, 10, 0, 12);
        doc.Add(operationalHeading);
        doc.Add(Body(
            "Revenue grew 18% year over year, led by double-digit gains across every " +
            "business line. The Platform division remained our largest contributor, while " +
            "Services posted its strongest quarter on record. Disciplined cost control let " +
            "us reinvest the upside into research, talent, and infrastructure."));
        doc.Add(SubHeading("Sustainability & Governance"));
        doc.Add(Body(
            "We reduced operational emissions by 22% and reached carbon neutrality across " +
            "our two largest facilities. Our board added two independent directors, " +
            "deepening the expertise that guides long-term strategy and risk oversight."));
        doc.Add(SubHeading("Looking Ahead"));
        doc.Add(Body(
            "Next year we will accelerate international expansion, deepen our product " +
            "platform, and continue returning value to shareholders. The fundamentals are " +
            "strong, the strategy is clear, and the team has never been more capable. We " +
            "are grateful for your continued partnership."));

        return doc;
    }

    // ── Background slot builders ────────────────────────────────────────────
    //
    // Page slots render one full-page element at the paper origin, so each
    // builder returns a single Container (nesting a second to mask out a
    // region). A flowing element such as VerticalStack is deliberately avoided
    // here: inside a slot it would apply page-break logic against the content
    // margins and mis-place its later children.

    /// <summary>Full-bleed deep-blue field for the cover.</summary>
    private static Container CoverBackground() => new Container
    {
        BackgroundColor = CoverBg,
        Height = PdfSize.A4.Height,
    };

    /// <summary>
    /// Full-bleed light bands in the top and bottom margins. A band-coloured
    /// page is masked by an inset white panel whose top and bottom edges carry
    /// brand-coloured hairlines, giving a crisp band/content separation. The
    /// header/footer text is centred onto the bands by their own slots.
    /// </summary>
    private static Container PageBands() => new Container
    {
        BackgroundColor = BandBg,
        Height = PdfSize.A4.Height,
        Child = new Container
        {
            BackgroundColor = Color.White,
            Height = PdfSize.A4.Height - Margin * 2,
            Margin = new Thickness(0, Margin, 0, Margin),
            Border = new Border(0, 2, 0, 2), // top + bottom hairlines only
            BorderColors = new BorderColors(null, Secondary, null, Tertiary),
        },
    };

    // ── Header / footer rows ────────────────────────────────────────────────

    private static TextBlock BandCell(string text, TextAlignment align, bool bold, Color color) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = bold ? 12 : 10,
        Color = color,
        Alignment = align,
    };

    private static HorizontalStack BandRow(
        TextBlock left, TextBlock center, TextBlock right, float leftWeight = 1, float centerWeight = 1,
        float rightWeight = 1)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(left, leftWeight);
        row.Add(center, centerWeight);
        row.Add(right, rightWeight);
        return row;
    }

    private static HorizontalStack HeaderRow(int pageNumber) => BandRow(
        BandCell("BonniePDF", TextAlignment.Left, bold: true, Primary),
        BandCell(ReportTitle, TextAlignment.Center, bold: false, ColorPalette.Blue[700]),
        BandCell($"Page {pageNumber}", TextAlignment.Right, bold: false, ColorPalette.Blue[600]));

    // The generation time needs a wider column than the page number, so the
    // footer leans on weighted columns rather than equal thirds.
    private static HorizontalStack FooterRow(int pageNumber) => BandRow(
        BandCell(Generated, TextAlignment.Left, bold: false, ColorPalette.Blue[600]),
        BandCell($"{Company} - Confidential", TextAlignment.Center, bold: false, ColorPalette.Blue[600]),
        BandCell($"Page {pageNumber}", TextAlignment.Right, bold: true, Primary),
        leftWeight: 5, centerWeight: 4, rightWeight: 2);

    // ── Cover content ───────────────────────────────────────────────────────

    /// <summary>A small three-square brand mark drawn from the colour trio.</summary>
    private static HorizontalStack BrandMark()
    {
        HorizontalStack row = new() { Spacing = 6 };
        foreach (Color color in new[] { Primary, Secondary, Tertiary })
        {
            row.Add(new Container { BackgroundColor = color, Width = 14, Height = 14, CornerRadius = 3 });
        }

        return row;
    }

    private static TextBlock CoverLabel(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = ColorPalette.Teal[300],
        Margin = new Thickness(0, 0, 0, 12),
    };

    /// <summary>The company name on the cover is the document's H1.</summary>
    private static Heading CoverTitle(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 42,
        Color = Color.White,
    };

    /// <summary>Three joined segments in the brand trio, sitting under the cover title.</summary>
    private static HorizontalStack TriColourRule()
    {
        HorizontalStack row = new() { Spacing = 0, Margin = new Thickness(0, 18, 0, 18) };
        foreach (Color color in new[] { Primary, Secondary, Tertiary })
        {
            row.Add(new Container { BackgroundColor = color, Width = 34, Height = 5 });
        }

        return row;
    }

    private static TextBlock CoverSubtitle(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 16,
        Color = ColorPalette.Blue[200],
    };

    private static TextBlock CoverCredit(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 10,
        Color = ColorPalette.Blue[300],
        Margin = new Thickness(0, 0, 0, 4),
    };

    // ── Inner content ───────────────────────────────────────────────────────

    private static Heading SectionHeading(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 20,
        Color = HeadingColor,
        Margin = new Thickness(0, 0, 0, 12),
    };

    private static Heading SubHeading(string text) => new(text)
    {
        Level = 3,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        Color = ColorPalette.Blue[700],
        Margin = new Thickness(0, 18, 0, 8),
    };

    private static TextBlock Body(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 11,
        Color = Ink,
        Alignment = TextAlignment.Justify,
        Margin = new Thickness(0, 0, 0, 12),
    };

    // ── KPI highlight cards ─────────────────────────────────────────────────

    private static HorizontalStack KpiRow()
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 14 };
        row.Add(KpiCard("£2.4B", "Total revenue", Primary), 1);
        row.Add(KpiCard("+18%", "Year-over-year growth", SecondaryText), 1);
        row.Add(KpiCard("2.1M", "Daily active users", TertiaryText), 1);
        return row;
    }

    private static Container KpiCard(string value, string label, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = value,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = accent,
            Margin = new Thickness(0, 0, 0, 4),
        });
        stack.Add(new TextBlock
        {
            Text = label,
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            // 600, not 500: this card sits on the Blue-50 panel, where 500 falls
            // to 4.38:1 against the 4.5:1 WCAG AA threshold.
            Color = ColorPalette.Slate[600],
        });

        return new Container
        {
            BackgroundColor = ColorPalette.Blue[50],
            CornerRadius = 10,
            Padding = new Thickness(18, 16, 18, 16),
            Child = stack,
        };
    }
}
