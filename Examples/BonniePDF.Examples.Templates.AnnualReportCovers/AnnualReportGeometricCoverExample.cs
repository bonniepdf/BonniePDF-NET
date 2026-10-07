using BonniePDF;
using BonniePDF.Drawing;

namespace BonniePDF.Examples.Templates.AnnualReportCovers;

/// <summary>
/// Annual-report front cover #3 - "Geometric badge".
///
/// A light, modern cover built from vector drawing primitives: a radial "sun" bleeding off
/// the top-left corner, an abstract cluster of translucent rotated polygons and a
/// star placed with affine transforms, and a circular masked image badge (a photo
/// cropped to a circle - the most common "team / hero" cover crop) ringed in gold.
///
/// The badge is a real photograph: the circular path is pushed as a clip and the
/// image drawn over its bounding square, so the circle does the cropping.
/// </summary>
[System.ComponentModel.DisplayName("Annual - Geometric")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description(
    "A two-page annual report. Page 1 is a light, modern cover built from vector primitives (radial sun, translucent rotated polygons and a star via affine transforms, a photo cropped to a circular badge ringed in gold). Page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportGeometricCoverExample
{
    private const string BadgeImageKey = "images/sean-pollock-PhYq704ffdA-unsplash.jpg";
    private const string Company = "Sample Company";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#0369a1", "#4f46e5", "#f59e0b", "#059669"];

    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Accent = ColorPalette.Amber[500];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.TryGetValue(BadgeImageKey, out byte[]? badgeData))
            throw new InvalidOperationException($"Missing required asset: {BadgeImageKey}");
        ImageInfo badgeImage = ImageLoader.Load(badgeData);

        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0, // zero margin - full-bleed cover
            Language = "en-GB",
            Accessibility = AccessibilityMode.PdfUa1,
            // The cover artwork lives in the page-background slot. Slots are painted
            // inside an /Artifact marked-content section, so the sun, the polygons
            // and the photo badge stay out of the structure tree where decoration
            // belongs - and the cover type above flows as ordinary tagged content.
            PageBackground = pageNumber => pageNumber == 1
                ? new CanvasElement((canvas, w, h) => DrawCover(canvas, w, h, badgeImage))
                {
                    DrawHeight = PdfSize.A4.Height,
                    IsDecorative = true,
                }
                : null,
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"{Company} - Annual Report {Year}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject =
            $"{Company} Annual Report {Year} - geometric cover with a circular masked image (vector drawing surface)";

        doc.Add(CoverType());

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        // Amber[700], not [500]: the KPI card paints this as 24pt bold text on the
        // Sky-50 tile, where [500] only reaches 2.00:1 against the 3:1 large-text
        // threshold. The cover's own Accent (canvas fills above) is unaffected.
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Sky[700], ColorPalette.Amber[700], ColorPalette.Sky[50],
            SegmentColors));

        return doc;
    }

    /// <summary>Regular polygon centred at (cx, cy), built in local coordinates.</summary>
    private static void AddPolygon(ICanvas canvas, float cx, float cy, float radius, int sides)
    {
        for (int i = 0; i < sides; i++)
        {
            float a = -MathF.PI / 2f + 2f * MathF.PI * i / sides;
            float x = cx + radius * MathF.Cos(a);
            float y = cy + radius * MathF.Sin(a);
            if (i == 0) canvas.MoveTo(x, y);
            else canvas.LineTo(x, y);
        }

        canvas.ClosePath();
    }

    /// <summary>Star centred at (cx, cy).</summary>
    private static void AddStar(ICanvas canvas, float cx, float cy, float outer, float inner, int points)
    {
        int total = points * 2;
        for (int i = 0; i < total; i++)
        {
            float a = -MathF.PI / 2f + MathF.PI * i / points;
            float r = i % 2 == 0 ? outer : inner;
            float x = cx + r * MathF.Cos(a);
            float y = cy + r * MathF.Sin(a);
            if (i == 0) canvas.MoveTo(x, y);
            else canvas.LineTo(x, y);
        }

        canvas.ClosePath();
    }

    private static void DrawCover(ICanvas canvas, float w, float h, ImageInfo badgeImage)
    {
        // 1. Light backdrop.
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, h);
        canvas.FillCurrentPath(ColorPalette.Slate[50]);

        // 2. Radial "sun" bleeding off the top-left corner.
        canvas.BeginPath();
        canvas.AddCircle(30f, 40f, 340f);
        canvas.FillCurrentPath(new RadialGradientPaint(
            new Point(30f, 40f), 340f,
            new[]
            {
                new GradientStop(0f, ColorPalette.Amber[300].WithAlpha(0.7f)),
                new GradientStop(1f, ColorPalette.Amber[300].WithAlpha(0f)),
            }));

        // 3. Abstract geometric cluster, lower-right, placed with transforms + alpha.
        canvas.SaveState();
        canvas.SetGlobalAlpha(0.5f);

        canvas.SaveState();
        canvas.Translate(w * 0.78f, h * 0.7f);
        canvas.Rotate(18f);
        canvas.BeginPath();
        AddPolygon(canvas, 0, 0, 120f, 6);
        canvas.FillCurrentPath(ColorPalette.Sky[400]);
        canvas.RestoreState();

        canvas.SaveState();
        canvas.Translate(w * 0.62f, h * 0.82f);
        canvas.Rotate(-12f);
        canvas.BeginPath();
        AddPolygon(canvas, 0, 0, 90f, 3);
        canvas.FillCurrentPath(ColorPalette.Violet[400]);
        canvas.RestoreState();

        canvas.SaveState();
        canvas.Translate(w * 0.86f, h * 0.86f);
        canvas.Rotate(8f);
        canvas.BeginPath();
        AddStar(canvas, 0, 0, 70f, 30f, 5);
        canvas.FillCurrentPath(Accent);
        canvas.RestoreState();

        canvas.RestoreState();

        // 4. Circular masked image badge, upper-centre.
        float bx = w * 0.5f;
        float by = h * 0.36f;
        float br = 96f;

        canvas.SaveState();
        canvas.BeginPath();
        canvas.AddCircle(bx, by, br);
        canvas.ClipToCurrentPath();

        // The photo is drawn over the badge's bounding square; the circular clip
        // above is what crops it to the circle.
        canvas.DrawImage(badgeImage, bx - br, by - br, br * 2f, br * 2f);

        canvas.RestoreState();

        // Gold ring around the badge.
        canvas.BeginPath();
        canvas.AddCircle(bx, by, br);
        canvas.StrokeCurrentPath(new StrokeStyle { Paint = Accent, Width = 5f });
    }

    /// <summary>
    /// The cover type as real text elements rather than canvas glyphs: the company
    /// name is the document's H1 and everything here lands in the structure tree,
    /// so a screen reader reads the cover in order instead of meeting one opaque
    /// figure.
    /// </summary>
    private static Container CoverType()
    {
        VerticalStack stack = new();

        stack.Add(new TextBlock($"ANNUAL REPORT {Year}")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = ColorPalette.Amber[600],
            Alignment = TextAlignment.Center,
        });

        stack.Add(new Heading(Company)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 44,
            LineHeight = 1f,
            Color = Ink,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 48),
        });

        stack.Add(new TextBlock("Stewardship, growth, and long-term value")
        {
            FontSize = 13,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center,
            // The block is anchored to the foot of the page, so this gap is what
            // sets how far the type sits below the badge: the eyebrow was clearing
            // the gold ring by about 3pt, which read as a collision.
            Margin = new Thickness(0, 0, 0, 240),
        });

        // Slate[600], not [400]: this footer sits on the flat page background,
        // where Slate[400] is 2.51:1 against the 4.5:1 WCAG AA threshold.
        stack.Add(new TextBlock($"Prepared with BonniePDF · Fiscal Year {Year}")
        {
            FontSize = 10,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
        });

        // Anchored to the foot of the page rather than positioned by a top padding
        // measured against the font's metrics: the block keeps its footing if the
        // title wraps or a face changes.
        return new Container
        {
            Height = PdfSize.A4.Height,
            VerticalAlignment = VerticalAlignment.End,
            Padding = new Thickness(56, 0, 56, 48),
            Child = stack,
        };
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    /// <summary>
    /// The brand palette a cover hands to the shared capabilities page so the
    /// page-2 spread picks up the same colours as its cover.
    /// </summary>
    private sealed record ReportBrand(string Company, int Year, Color Primary, Color Accent, Color Tint, string[] SegmentColors);

    /// <summary>
    /// Shared "inside the report" capabilities spread for the annual-report covers.
    ///
    /// Each cover example is a single full-bleed page of artwork. On its own that
    /// sells the look; it does not prove what BonniePDF can lay out inside a real
    /// annual report. This appends a second page that does exactly that - a KPI
    /// band, a quarterly-revenue column chart, a revenue-by-segment donut, and a
    /// financial-summary table - so every cover showcase reads as a two-page report.
    /// The charts deliberately use the full editorial column. Annual-report charts
    /// need room for axes, legends and data labels; squeezing two plots into equal
    /// half-page cards makes the decoration win over the information.
    ///
    /// The covers use a zero-margin document (the artwork bleeds to the paper edge),
    /// so this page supplies its own inset via a padded container.
    /// </summary>
    private static class CapabilitiesPage
    {
        private static readonly Color Ink = ColorPalette.Slate[700];
        // 600, not 500: this label sits on each cover's own brand.Tint panel, and 500
        // falls to 4.26-4.47:1 there against the 4.5:1 WCAG AA threshold.
        private static readonly Color Muted = ColorPalette.Slate[600];
        private static readonly Color Rule = ColorPalette.Slate[200];
        /// <summary>Appends the capabilities spread as a new page on <paramref name = "doc"/>.</summary>
        public static void Add(Document doc, ReportBrand brand)
        {
            doc.AddPage();
            VerticalStack content = new();
            content.Add(new TextBlock { Text = "INSIDE THE REPORT", FontName = StandardFonts.HelveticaBold, FontSize = 9, Color = brand.Accent, Margin = new Thickness(0, 0, 0, 4), });
            // A real H2 under the cover's H1: the section title carries structure, not
            // just 22pt of bold type.
            content.Add(new Heading("Performance at a glance") { Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 22, Color = brand.Primary, Margin = new Thickness(0, 0, 0, 6), });
            content.Add(new TextBlock { Text = $"{brand.Company}'s {brand.Year} results, laid out the way an annual report presents " + "them: headline metrics, trend and mix charts, and an audited summary table.", FontName = StandardFonts.Helvetica, FontSize = 10, Color = Muted, Margin = new Thickness(0, 0, 0, 18), });
            // KPI band
            HorizontalStack kpis = new()
            {
                WidthDistribution = WidthDistribution.Weighted,
                Spacing = 12,
                Margin = new Thickness(0, 0, 0, 18),
            };
            kpis.Add(KpiCard("£988M", "Total revenue", brand.Primary, brand.Tint), 1);
            kpis.Add(KpiCard("+18%", "Year-over-year growth", brand.Accent, brand.Tint), 1);
            kpis.Add(KpiCard("21.4%", "Operating margin", brand.Primary, brand.Tint), 1);
            content.Add(kpis);
            // Full-width editorial charts. The chart implementations reserve space for
            // axes and a right-hand legend, so two half-width cards leave almost no plot
            // area and can force legend content beyond the card. Stacking mirrors real
            // annual-report layouts: one clear visual per reading row.
            VerticalStack charts = new()
            {
                Spacing = 12,
                Margin = new Thickness(0, 0, 0, 18),
            };
            charts.Add(ChartCard("Quarterly revenue (£m)", QuarterlyBarChart(brand.Primary), brand.Accent));
            charts.Add(ChartCard("Revenue by segment", SegmentDonut(brand.SegmentColors), brand.Accent));
            content.Add(charts);
            // Financial summary table
            content.Add(new Heading("Five-year financial summary") { Level = 3, FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = brand.Accent, Margin = new Thickness(0, 0, 0, 8), });
            content.Add(FinancialTable(brand.Primary));
            // The cover documents use a zero margin, so inset the page ourselves.
            doc.Add(new Container { Padding = new Thickness(56, 48, 56, 48), Child = content, });
        }

        private static Container KpiCard(string value, string label, Color accent, Color tint)
        {
            VerticalStack stack = new();
            stack.Add(new TextBlock { Text = value, FontName = StandardFonts.HelveticaBold, FontSize = 22, Color = accent, Margin = new Thickness(0, 0, 0, 3), });
            stack.Add(new TextBlock { Text = label, FontName = StandardFonts.Helvetica, FontSize = 9, Color = Muted, });
            return new Container
            {
                BackgroundColor = tint,
                CornerRadius = 8,
                Padding = new Thickness(16, 14, 16, 14),
                Child = stack,
            };
        }

        private static Container ChartCard(string title, IElement body, Color accent)
        {
            VerticalStack stack = new();
            stack.Add(new TextBlock { Text = title, FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = accent, Margin = new Thickness(0, 0, 0, 8), });
            stack.Add(body);
            return new Container
            {
                Border = new Border(0.75f),
                BorderColors = new BorderColors(Rule),
                CornerRadius = 8,
                ClipContent = true,
                Padding = new Thickness(14, 14, 14, 14),
                Child = stack,
            };
        }

        private static BarChart QuarterlyBarChart(Color primary)
        {
            string[] labels = ["Q1", "Q2", "Q3", "Q4"];
            decimal[] values = [182, 205, 241, 288];
            ChartSeries series = new()
            {
                Color = primary
            };
            for (int i = 0; i < labels.Length; i++)
            {
                series.DataPoints.Add(new ChartDataPoint { Label = labels[i], Value = values[i] });
            }

            ChartData data = new()
            {
                Series = [series],
                YAxis = new AxisOptions
                {
                    Prefix = "£",
                    MaxValue = 320,
                    StepCount = 4
                },
            };
            BarChart chart = new(data)
            {
                AltText = "Column chart of quarterly revenue in £m rising from 182 in Q1 to 288 in Q4.",
                Height = 130,
                StretchToFit = true,
                BarColor = primary,
                FontFamily = StandardFonts.Helvetica,
                LabelFontSize = 8,
                BackgroundColor = Color.White,
            };
            return chart;
        }

        private static PieChart SegmentDonut(string[] colors)
        {
            string[] labels = ["Platform", "Services", "Hardware", "Licensing"];
            decimal[] values = [512, 274, 118, 84];
            ChartSeries series = new();
            for (int i = 0; i < labels.Length; i++)
            {
                series.DataPoints.Add(new ChartDataPoint { Label = labels[i], Value = values[i] });
            }

            ChartData data = new()
            {
                Series = [series]
            };
            PieChart chart = new(data)
            {
                AltText = "Donut chart of revenue by segment: Platform, Services, Hardware, and Licensing.",
                Height = 130,
                StretchToFit = true,
                IsDonut = true,
                TotalLabel = "Total",
                ShowLegend = true,
                Colors = colors,
                BackgroundColor = Color.White,
            };
            return chart;
        }

        private static Table FinancialTable(Color primary)
        {
            Table table = new(2.2f, 1f, 1f, 0.9f)
            {
                CellPadding = 8,
                Border = Border.None,
                HeaderBackgroundColor = primary,
                HeaderTextColor = Color.White,
            };
            table.AddHeader(HeaderCell("£ millions", TextAlignment.Left), HeaderCell("FY25", TextAlignment.Right), HeaderCell("FY24", TextAlignment.Right), HeaderCell("Change", TextAlignment.Right));
            (string Name, string Cur, string Prev, string Chg)[] rows = [("Revenue", "988", "837", "+18%"), ("Gross profit", "641", "523", "+23%"), ("Operating income", "212", "164", "+29%"), ("Net income", "167", "129", "+29%"), ("Earnings per share", "£1.34", "£1.04", "+29%"), ];
            foreach ((string name, string cur, string prev, string chg)in rows)
            {
                table.AddRow(BodyCell(name, Ink, TextAlignment.Left, false), BodyCell(cur, Ink, TextAlignment.Right, false), BodyCell(prev, Muted, TextAlignment.Right, false), BodyCell(chg, ColorPalette.Emerald[600], TextAlignment.Right, true));
            }

            return table;
        }

        private static TextBlock HeaderCell(string text, TextAlignment align) => new()
        {
            Text = text,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            Color = Color.White,
            Alignment = align,
        };
        private static TextBlock BodyCell(string text, Color color, TextAlignment align, bool bold) => new()
        {
            Text = text,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = 9,
            Color = color,
            Alignment = align,
        };
    }
}
