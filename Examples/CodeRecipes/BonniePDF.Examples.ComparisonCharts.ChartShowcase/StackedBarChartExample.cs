using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

[System.ComponentModel.DisplayName("Stacked Bar Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "Multi-series stacked bars showing quarterly revenue by acquisition channel, with category stack totals, segment gaps, and auto-contrast inside labels.")]
public static class StackedBarChartExample
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
                    Text = "BonniePDF - Stacked Bar Chart",
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

        document.Metadata.Title = "Stacked Bar Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Stacked bar chart example - quarterly revenue split by acquisition channel";

        document.Add(new TextBlock
        {
            Text = "Stacked Bar Chart: Revenue Mix by Channel",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "Multi-series stacking with compact K totals, segment gaps, and auto-contrast inside labels",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20),
        });

        ChartData data = new()
        {
            Title = "Quarterly Revenue Mix",
            YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K0" },
            Series =
            [
                new ChartSeries
                {
                    Name = "Direct",
                    DataPoints = [new() { Label = "Q1", Value = 180000 }, new() { Label = "Q2", Value = 215000 }, new() { Label = "Q3", Value = 248000 }, new() { Label = "Q4", Value = 292000 }],
                },
                new ChartSeries
                {
                    Name = "Partner",
                    DataPoints = [new() { Label = "Q1", Value = 95000 }, new() { Label = "Q2", Value = 112000 }, new() { Label = "Q3", Value = 128000 }, new() { Label = "Q4", Value = 146000 }],
                },
                new ChartSeries
                {
                    Name = "Self-serve",
                    DataPoints = [new() { Label = "Q1", Value = 64000 }, new() { Label = "Q2", Value = 78000 }, new() { Label = "Q3", Value = 94000 }, new() { Label = "Q4", Value = 118000 }],
                },
            ],
        };

        document.Add(new StackedBarChart(data)
        {
            Theme = "modern",
            Mode = "absolute",
            ShowStackTotals = true,
            StackTotalFormat = "K0",
            ValueLabelFormat = "K0",
            ValueLabelPrefix = "£",
            SegmentGap = 1.5f,
            AltText = "Stacked bar chart showing quarterly revenue split across Direct, Partner, and Self-serve channels, rising from 339K in Q1 to 556K in Q4.",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            BackgroundColor = Color.White,
        });

        return document;
    }
}
