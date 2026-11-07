using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.ShareRankingCharts.ShareAndRanking;

/// <summary>
/// Example demonstrating the LollipopChart implementation.
/// This shows how a LollipopChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Lollipop Chart")]
[System.ComponentModel.Category("Share & Ranking Charts")]
[System.ComponentModel.Description(
    "LollipopChart implementation with twelve months of revenue, showing data points as lollipops with customizable stem width and head radius.")]
public static class LollipopChartExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        // Create a new PDF document
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack stack = new() { WidthDistribution = WidthDistribution.Weighted };
                stack.Add(
                    new TextBlock
                    {
                        Text = "BonniePDF - Lollipop Chart", FontName = StandardFonts.Helvetica, FontSize = 10,
                        Color = ColorPalette.Slate[500]
                    }, null);
                stack.Add(
                    new TextBlock
                    {
                        Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                        FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500],
                        Alignment = TextAlignment.Right
                    }, 1f);
                return stack;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        document.Metadata.Title = "Lollipop Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the LollipopChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Lollipop Chart: Monthly Revenue",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Stems with circular heads - clear and minimal bar alternative",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        // Create chart data with twelve months of revenue
        ChartData chartData = new()
        {
            Title = "Monthly Revenue",
            Series =
            [
                new ChartSeries { Color = Color.FromHex("#ec4899"),
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Jan", Value = 1420, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Feb", Value = 1510, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Mar", Value = 1630, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Apr", Value = 1580, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "May", Value = 1720, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jun", Value = 1890, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jul", Value = 1960, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Aug", Value = 1840, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Sep", Value = 2030, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Oct", Value = 2210, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Nov", Value = 2380, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Dec", Value = 2640, LabelFormat = "N0" },
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 3000,
                StepCount = 6,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create the lollipop chart
        LollipopChart lollipopChart = new(chartData)
        {
            AltText = "Lollipop chart of monthly revenue in 2026, rising from 1,420 GBP in January to 2,640 GBP in December.",
            Height = 380,
            StretchToFit = true,
            LollipopColor = Color.FromHex("#ec4899"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            Theme = "modern",
            BackgroundColor = Color.White
        };

        // Add the chart to the document
        document.Add(lollipopChart);

        // Save the document


        return document;
    }

    // Local helpers and sample data for this document.


}
