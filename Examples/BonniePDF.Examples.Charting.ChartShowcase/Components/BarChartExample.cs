using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Example demonstrating the new refactored chart architecture.
/// This shows how a BarChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Bar Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description(
    "BarChart implementation with monthly sales report data, showing vertical bars with customizable colors and spacing.")]
public static class BarChartExample
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
                        Text = "BonniePDF - Bar Chart", FontName = StandardFonts.Helvetica, FontSize = 10,
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
        document.Settings.PdfAEnabled = true;
        document.Settings.Language = "en-GB";

        // Set document metadata
        document.Metadata.Title = "Bar Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the new ICanvas-based chart architecture";

        // Add a page
        // document.AddPage();

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Monthly Sales Report",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Vertical bars with grid lines and value labels",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales",
            Series =
            [
                new ChartSeries
                {
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Jan", Value = 4500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Feb", Value = 6200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Mar", Value = 5800, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Apr", Value = 7200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "May", Value = 8500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jun", Value = 9100, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jul", Value = 8200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Aug", Value = 7800, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Sep", Value = 9500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Oct", Value = 10200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Nov", Value = 11500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Dec", Value = 12800, LabelFormat = "N0" }
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5
            }
        };

        // Create the bar chart
        // StretchToFit = true will make the chart fill the container width
        BarChart barChart = new(chartData)
        {
            AltText = "Bar chart showing monthly sales report from January to December",
            Height = 400,
            StretchToFit = true,
            BarColor = Color.FromHex("#4f46e5"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Get the current page's content stream and page height
        // We need to use the PdfCanvas to draw the chart
        // For this example, we'll create a custom element that wraps the chart

        document.Add(barChart);

        // Save the document


        return document;
    }
}
