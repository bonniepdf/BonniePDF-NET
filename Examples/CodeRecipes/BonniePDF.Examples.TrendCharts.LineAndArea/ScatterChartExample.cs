using BonniePDF;

namespace BonniePDF.Examples.TrendCharts.LineAndArea;

[System.ComponentModel.DisplayName("Scatter Chart")]
[System.ComponentModel.Category("Trend Charts")]
[System.ComponentModel.Description(
    "Continuous numeric X/Y scatter plot comparing cluster latency against throughput, with area-scaled bubbles and OLS linear trendlines.")]
public static class ScatterChartExample
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
                    Text = "BonniePDF - Scatter Chart",
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

        document.Metadata.Title = "Scatter Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Scatter chart example - continuous X/Y coordinates with bubble sizing and linear trendline";

        document.Add(new TextBlock
        {
            Text = "Scatter Chart: Latency vs. Throughput",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "Continuous numeric X/Y coordinates with area-scaled bubbles and OLS linear trendline",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20),
        });

        ChartData data = new()
        {
            Title = "Cluster Scaling Profile",
            XAxis = new AxisOptions { Legend = "Requests / sec", AutoScale = true },
            YAxis = new AxisOptions { Legend = "p95 Latency (ms)", AutoScale = true },
            Series =
            [
                new ChartSeries
                {
                    Name = "Region EU",
                    DataPoints =
                    [
                        new() { Label = "n1", X = 120, Value = 14, Size = 12 },
                        new() { Label = "n2", X = 240, Value = 21, Size = 22 },
                        new() { Label = "n3", X = 360, Value = 29, Size = 34 },
                        new() { Label = "n4", X = 480, Value = 41, Size = 48 },
                    ],
                },
                new ChartSeries
                {
                    Name = "Region US",
                    DataPoints =
                    [
                        new() { Label = "u1", X = 150, Value = 18, Size = 15 },
                        new() { Label = "u2", X = 290, Value = 27, Size = 28 },
                        new() { Label = "u3", X = 420, Value = 37, Size = 40 },
                        new() { Label = "u4", X = 520, Value = 49, Size = 54 },
                    ],
                },
            ],
        };

        document.Add(new ScatterChart(data)
        {
            Theme = "modern",
            ShowTrendline = "linear",
            AltText = "Scatter bubble chart of p95 latency versus throughput for EU and US regions, with bubble size proportional to active nodes and linear trendlines.",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            BackgroundColor = Color.White,
        });

        return document;
    }
}
