using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.Charting.ChartExamples.Components;

/// <summary>
/// Example demonstrating the LollipopChart implementation.
/// This shows how a LollipopChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Lollipop Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description(
    "LollipopChart implementation with quarterly performance metrics, showing data points as lollipops with customizable stem width and head radius.")]
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
            Text = "Lollipop Chart: Quarterly Metrics",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Stems with circular heads - clear and minimal bar alternative",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        // Create chart data with quarterly data
        ChartData chartData = new()
        {
            Title = "Quarterly Revenue",
            Series =
            [
                new ChartSeries { Color = Color.FromHex("#ec4899"),
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Q1", Value = 4500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Q2", Value = 6200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Q3", Value = 8500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Q4", Value = 11500, LabelFormat = "N0" }
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Quarter"
            }
        };

        // Create the lollipop chart
        LollipopChart lollipopChart = new(chartData)
        {
            AltText = "Lollipop chart showing quarterly revenue performance",
            Height = 380,
            StretchToFit = true,
            LollipopColor = Color.FromHex("#ec4899"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(lollipopChart);

        // Save the document


        return document;
    }

    // Local helpers and sample data for this document.


}
