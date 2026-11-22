using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

[System.ComponentModel.DisplayName("Combo Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "Primary revenue bars paired with a smooth gross-margin percentage line on a secondary right-hand Y axis.")]
public static class ComboChartExample
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
                    Text = "BonniePDF - Combo Chart",
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

        document.Metadata.Title = "Combo Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Combo chart example - revenue bars on primary axis and gross margin line on secondary axis";

        document.Add(new TextBlock
        {
            Text = "Combo Chart: Revenue vs. Gross Margin",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "Primary revenue bars paired with a smooth gross-margin percentage line on a secondary Y axis",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20),
        });

        ChartData data = new()
        {
            Title = "Quarterly Revenue & Gross Margin",
            YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K0" },
            Series =
            [
                new ChartSeries
                {
                    Name = "Revenue",
                    SeriesType = "bar",
                    ValueAxis = "primary",
                    DataPoints = [new() { Label = "Q1", Value = 420000 }, new() { Label = "Q2", Value = 485000 }, new() { Label = "Q3", Value = 540000 }, new() { Label = "Q4", Value = 635000 }],
                },
                new ChartSeries
                {
                    Name = "Gross Margin",
                    SeriesType = "line",
                    ValueAxis = "secondary",
                    Color = Color.FromHex("#f59e0b"),
                    Style = new ChartSeriesStyle { Curve = "smooth", LineWidth = 2.5f, MarkerFill = "hollow", MarkerHaloWidth = 1.5f },
                    DataPoints = [new() { Label = "Q1", Value = 68 }, new() { Label = "Q2", Value = 71 }, new() { Label = "Q3", Value = 74 }, new() { Label = "Q4", Value = 79 }],
                },
            ],
        };

        document.Add(new ComboChart(data)
        {
            Theme = "modern",
            ShowValueLabels = false,
            LegendPosition = "top",
            SecondaryYAxis = new AxisOptions { MinValue = 50, MaxValue = 90, Suffix = "%", ValueFormat = "F0" },
            AltText = "Combo chart comparing quarterly revenue bars from 420K to 635K GBP against gross margin rising from 68% to 79% on a secondary axis.",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            BackgroundColor = Color.White,
        });

        return document;
    }
}
