using BonniePDF;
using BonniePDF.Drawing;

namespace BonniePDF.Examples.Templates.AnnualReportCovers;

/// <summary>
/// Annual-report front cover #1 - "Gradient field".
///
/// A full-bleed annual-report cover painted entirely on the vector drawing
/// surface. The whole page is one <see cref="CanvasElement"/>: a vertical
/// linear-gradient backdrop, a soft radial "glow" that fades to transparent, a
/// set of fine diagonal accent rules, a rotated brand mark, and the cover type
/// drawn directly with canvas text.
///
/// Because the cover is one canvas the document uses a zero margin and a single
/// full-page element, so the artwork bleeds to every paper edge - exactly how a
/// printed report cover is laid out.
/// </summary>
[System.ComponentModel.DisplayName("Annual - Gradient")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description(
    "A two-page annual report. Page 1 is a full-bleed cover drawn on the vector surface (indigo-to-fuchsia gradient, radial glow, diagonal accent rules, rotated brand mark, canvas-drawn type); page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportGradientCoverExample
{
    private const string Company = "Sample Company";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#4f46e5", "#7c3aed", "#c026d3", "#f59e0b"];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0, // zero margin - full-bleed cover
            Language = "en-GB",
            Accessibility = AccessibilityMode.PdfUa1,
            // The cover artwork lives in the page-background slot. Slots are painted
            // inside an /Artifact marked-content section, so the gradient, the glow
            // and the accent rules stay out of the structure tree where decoration
            // belongs - and the cover type above flows as ordinary tagged content.
            PageBackground = pageNumber => pageNumber == 1
                ? new CanvasElement(DrawCover)
                {
                    DrawHeight = PdfSize.A4.Height,
                    IsDecorative = true,
                }
                : null,
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"{Company} - Annual Report {Year}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - gradient cover (vector drawing surface)";

        doc.Add(CoverType());

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Indigo[700], ColorPalette.Fuchsia[600], ColorPalette.Indigo[50],
            SegmentColors));

        return doc;
    }

    private static void DrawCover(ICanvas canvas, float w, float h)
    {
        // 1. Full-bleed vertical gradient backdrop (deep indigo -> violet -> fuchsia).
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, h);
        canvas.FillCurrentPath(new LinearGradientPaint(
            new Point(0, 0), new Point(0, h),
            new GradientStop(0f, ColorPalette.Indigo[950]),
            new GradientStop(0.55f, ColorPalette.Violet[800]),
            new GradientStop(1f, ColorPalette.Fuchsia[700])));

        // 2. Soft radial glow, upper-right, fading to transparent so it reads as
        //    light bleeding across the gradient rather than a hard disc.
        canvas.SaveState();
        canvas.BeginPath();
        canvas.AddCircle(w * 0.84f, h * 0.16f, 260f);
        canvas.FillCurrentPath(new RadialGradientPaint(
            new Point(w * 0.84f, h * 0.16f), 260f,
            new[]
            {
                new GradientStop(0f, ColorPalette.Amber[200].WithAlpha(0.55f)),
                new GradientStop(1f, ColorPalette.Amber[200].WithAlpha(0f)),
            }));
        canvas.RestoreState();

        // 3. Fine diagonal accent rules across the lower third, drawn at low alpha.
        canvas.SaveState();
        canvas.SetGlobalAlpha(0.35f);
        for (int i = 0; i < 5; i++)
        {
            float y = h * 0.62f + i * 16f;
            canvas.BeginPath();
            canvas.MoveTo(0, y);
            canvas.LineTo(w, y - 60f);
            canvas.StrokeCurrentPath(new StrokeStyle
                { Paint = ColorPalette.Fuchsia[300], Width = 1.5f, LineCap = LineCap.Round });
        }

        canvas.RestoreState();

        // 4. Brand mark - a single gold accent square rotated 45 degrees (a transform demo).
        canvas.SaveState();
        canvas.Translate(72f, h * 0.2f);
        canvas.Rotate(45f);
        canvas.BeginPath();
        canvas.AddRect(-9f, -9f, 18f, 18f);
        canvas.FillCurrentPath(ColorPalette.Amber[400]);
        canvas.RestoreState();

        // The gold rule under the title is decoration, so it stays a drawn box.
        canvas.BeginPath();
        canvas.AddRect(74f, h * 0.49f, 120f, 5f);
        canvas.FillCurrentPath(ColorPalette.Amber[400]);
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

        stack.Add(new TextBlock("SAMPLE")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = Color.White,
            Margin = new Thickness(36, 0, 0, 108),
        });

        stack.Add(new TextBlock($"ANNUAL REPORT {Year}")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Fuchsia[200],
            Margin = new Thickness(0, 0, 0, 8),
        });

        stack.Add(new Heading(Company)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 52,
            LineHeight = 1f,
            Color = Color.White,
            Margin = new Thickness(0, 0, 0, 70),
        });

        stack.Add(new TextBlock("Designing the systems that power tomorrow")
        {
            FontSize = 14,
            Color = ColorPalette.Violet[100],
            Margin = new Thickness(0, 0, 0, 324),
        });

        // Violet[100], not [200]/[300]: this footer sits near the foot of the
        // page, where the background gradient has shifted to its brightest stop
        // (#a800b7). Violet[300] is 3.38:1 there - well under 4.5:1 - and
        // Violet[200] is only a rounding error above it (4.52:1). Violet[100],
        // already used for the headline above, holds a comfortable 5.28:1 at
        // that same point.
        stack.Add(new TextBlock("Prepared with BonniePDF")
        {
            FontSize = 10,
            Color = ColorPalette.Violet[100],
            Margin = new Thickness(0, 0, 0, 7),
        });

        stack.Add(new TextBlock($"Fiscal Year {Year} · Confidential")
        {
            FontSize = 10,
            Color = ColorPalette.Violet[100],
        });

        // Anchored to the foot of the page rather than positioned by a top padding
        // measured against the font's metrics: the block keeps its footing if the
        // title wraps or a face changes.
        return new Container
        {
            Height = PdfSize.A4.Height,
            VerticalAlignment = VerticalAlignment.End,
            Padding = new Thickness(72, 0, 72, 51),
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
    /// financial-summary table - so every cover examples reads as a two-page report.
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
