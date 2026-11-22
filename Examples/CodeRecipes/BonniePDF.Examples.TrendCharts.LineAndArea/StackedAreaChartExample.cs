using BonniePDF;

namespace BonniePDF.Examples.TrendCharts.LineAndArea;

[System.ComponentModel.DisplayName("Stacked Area Chart")]
[System.ComponentModel.Category("Trend Charts")]
[System.ComponentModel.Description(
    "100% normalized smooth stacked area chart showing platform traffic share across Web, Mobile, and API with inline series labels.")]
public static class StackedAreaChartExample
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
                    Text = "BonniePDF - Stacked Area Chart",
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

        document.Metadata.Title = "Stacked Area Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Stacked area chart example - 100% normalized platform traffic share";

        document.Add(new TextBlock
        {
            Text = "Stacked Area Chart: Platform Traffic Share",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "100% normalized smooth stacked area bands with inline series labels",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20),
        });

        ChartData data = new()
        {
            Title = "Traffic Share (%)",
            YAxis = new AxisOptions { Suffix = "%" },
            Series =
            [
                new ChartSeries { Name = "Web", DataPoints = [new() { Label = "Q1", Value = 58 }, new() { Label = "Q2", Value = 54 }, new() { Label = "Q3", Value = 50 }, new() { Label = "Q4", Value = 47 }] },
                new ChartSeries { Name = "Mobile", DataPoints = [new() { Label = "Q1", Value = 30 }, new() { Label = "Q2", Value = 33 }, new() { Label = "Q3", Value = 36 }, new() { Label = "Q4", Value = 38 }] },
                new ChartSeries { Name = "API", DataPoints = [new() { Label = "Q1", Value = 12 }, new() { Label = "Q2", Value = 13 }, new() { Label = "Q3", Value = 14 }, new() { Label = "Q4", Value = 15 }] },
            ],
        };

        document.Add(new StackedAreaChart(data)
        {
            Theme = "modern",
            Mode = "percent",
            Curve = "smooth",
            LegendPosition = "inline",
            AltText = "100% stacked area chart showing platform traffic share shifting from Web (58% to 47%) toward Mobile (30% to 38%) and API (12% to 15%).",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            BackgroundColor = Color.White,
        });

        return document;
    }
}
