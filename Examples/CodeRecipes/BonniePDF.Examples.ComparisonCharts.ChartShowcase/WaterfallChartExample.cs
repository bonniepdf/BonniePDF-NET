using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

[System.ComponentModel.DisplayName("Waterfall Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "Cumulative annual recurring revenue movement bridge with positive/negative deltas, auto-computed ending total, and dashed connectors.")]
public static class WaterfallChartExample
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
                    Text = "BonniePDF - Waterfall Chart",
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

        document.Metadata.Title = "Waterfall Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Waterfall chart example - cumulative ARR variance bridge with totals and connectors";

        document.Add(new TextBlock
        {
            Text = "Waterfall Chart: ARR Movement Bridge",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "Cumulative variance bridge with positive/negative deltas, auto-computed ending total, and connectors",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20),
        });

        ChartData data = new()
        {
            Title = "Annual Recurring Revenue Bridge",
            YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K1" },
            Series =
            [
                new ChartSeries
                {
                    Name = "ARR",
                    DataPoints =
                    [
                        new() { Label = "Start", Value = 800000, Kind = "total" },
                        new() { Label = "New", Value = 260000, Kind = "delta" },
                        new() { Label = "Expansion", Value = 135000, Kind = "delta" },
                        new() { Label = "Churn", Value = -85000, Kind = "delta" },
                        new() { Label = "Contraction", Value = -40000, Kind = "delta" },
                        new() { Label = "Ending", Kind = "total" },
                    ],
                },
            ],
        };

        document.Add(new WaterfallChart(data)
        {
            Theme = "modern",
            ValueLabelPrefix = "£",
            ValueLabelFormat = "K1",
            AltText = "Waterfall bridge from 800k starting ARR through new business, expansion, churn, and contraction to 1.07M ending ARR.",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            BackgroundColor = Color.White,
        });

        return document;
    }
}
