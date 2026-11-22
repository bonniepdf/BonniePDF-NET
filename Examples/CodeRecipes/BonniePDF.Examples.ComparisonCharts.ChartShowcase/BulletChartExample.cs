using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

[System.ComponentModel.DisplayName("Bullet Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "Executive KPI scorecard with three horizontal bullet gauges showing qualitative background bands, primary measure bars, and target markers.")]
public static class BulletChartExample
{
    public static Document Generate()
    {
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = _ =>
            {
                HorizontalStack stack = new() { WidthDistribution = WidthDistribution.Weighted };
                stack.Add(new TextBlock
                {
                    Text = "BonniePDF - Bullet Chart",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    Color = ColorPalette.Slate[500],
                }, null);
                stack.Add(PageNumber.Text().Font(StandardFonts.Helvetica, 10).Color(ColorPalette.Slate[500]).AlignRight(), 1f);
                return stack;
            },
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);

        document.Metadata.Title = "Bullet Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Bullet chart example - executive KPI gauges with qualitative bands and targets";

        document.Add(new TextBlock
        {
            Text = "Bullet Chart: Executive KPI Scorecard",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "Compact KPI gauges with qualitative background ranges, primary measure bars, and target markers",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 24),
        });

        VerticalStack kpis = new() { Spacing = 20 };

        HorizontalStack row1 = new() { Spacing = 16, WidthDistribution = WidthDistribution.Weighted };
        row1.Add(new BulletChart
        {
            Theme = "modern",
            Label = "Revenue (£k)",
            Value = 275,
            Target = 250,
            Ranges = [new() { Max = 150 }, new() { Max = 225 }, new() { Max = 300 }],
            ShowScale = true,
            ShowValueLabel = true,
            ValuePrefix = "£",
            ValueSuffix = "k",
            BorderRadius = 6,
            BorderColor = Color.FromHex("#e2e8f0"),
            BorderWidth = 1,
            Width = 520,
            Height = 68,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Bullet chart showing revenue at 275k against a 250k target.",
        }, 3f);
        row1.Add(KpiSparkline(
            "#2563eb",
            250,
            [195, 204, 212, 208, 224, 236, 241, 248, 255, 262, 269, 275],
            "Twelve-month revenue sparkline rising from 195k to 275k against a 250k target."), 1f);
        kpis.Add(row1);

        HorizontalStack row2 = new() { Spacing = 16, WidthDistribution = WidthDistribution.Weighted };
        row2.Add(new BulletChart
        {
            Theme = "modern",
            Label = "Gross Margin",
            Value = 74,
            Target = 78,
            Ranges = [new() { Max = 50 }, new() { Max = 75 }, new() { Max = 100 }],
            ShowScale = true,
            ShowValueLabel = true,
            ValueSuffix = "%",
            MeasureColor = Color.FromHex("#0d9488"),
            BorderRadius = 6,
            BorderColor = Color.FromHex("#e2e8f0"),
            BorderWidth = 1,
            Width = 520,
            Height = 68,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Bullet chart showing gross margin at 74% against a 78% target.",
        }, 3f);
        row2.Add(KpiSparkline(
            "#0d9488",
            78,
            [68, 69, 71, 70, 72, 73, 71, 72, 73, 75, 74, 74],
            "Twelve-month gross margin sparkline trending from 68% to 74% against a 78% target."), 1f);
        kpis.Add(row2);

        HorizontalStack row3 = new() { Spacing = 16, WidthDistribution = WidthDistribution.Weighted };
        row3.Add(new BulletChart
        {
            Theme = "modern",
            Label = "Net Promoter",
            Value = 64,
            Target = 60,
            Ranges = [new() { Max = 30 }, new() { Max = 55 }, new() { Max = 80 }],
            ShowScale = true,
            ShowValueLabel = true,
            MeasureColor = Color.FromHex("#7c3aed"),
            BorderRadius = 6,
            BorderColor = Color.FromHex("#e2e8f0"),
            BorderWidth = 1,
            Width = 520,
            Height = 68,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Bullet chart showing Net Promoter Score at 64 against a target of 60.",
        }, 3f);
        row3.Add(KpiSparkline(
            "#7c3aed",
            60,
            [51, 53, 52, 55, 57, 58, 59, 61, 60, 62, 63, 64],
            "Twelve-month Net Promoter Score sparkline climbing from 51 to 64 against a 60 target."), 1f);
        kpis.Add(row3);

        document.Add(kpis);

        return document;
    }

    private static readonly string[] Months =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private static LineChart KpiSparkline(string color, decimal target, decimal[] values, string altText)
    {
        ChartSeries series = new() { Color = Color.FromHex(color) };
        for (int i = 0; i < Months.Length; i++)
        {
            series.DataPoints.Add(new ChartDataPoint { Label = Months[i], Value = values[i] });
        }

        return new LineChart(new ChartData { Series = [series] })
        {
            Theme = "modern",
            Sparkline = true,
            Curve = "smooth",
            LineColor = Color.FromHex(color),
            SparklineFillOpacity = 0.18f,
            SparklineShowEndpoints = true,
            SparklineHighlightMinMax = true,
            SparklineReferenceValue = target,
            BorderRadius = 6,
            BorderColor = Color.FromHex("#e2e8f0"),
            BorderWidth = 1,
            Height = 68,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = altText,
        };
    }
}
