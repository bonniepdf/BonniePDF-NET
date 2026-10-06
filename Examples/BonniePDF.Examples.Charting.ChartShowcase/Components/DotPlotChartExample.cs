using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Example demonstrating the DotPlotChart implementation.
/// This shows how a DotPlotChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Dot Plot Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("DotPlotChart implementation with product performance data, showing dots at data points with customizable radius and colors.")]
public static class DotPlotChartExample
{

    public static Document Generate()
    {
        // Create a new PDF document
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack stack = new() { WidthDistribution = WidthDistribution.Weighted };
                stack.Add(new TextBlock { Text = "BonniePDF - Dot Plot Chart", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500] }, null);
                stack.Add(new TextBlock { Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500], Alignment = TextAlignment.Right }, 1f);
                return stack;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;
        document.Settings.Language = "en-GB";

        // Set document metadata
        document.Metadata.Title = "Dot Plot Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the DotPlotChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Product Performance Analysis",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Circular markers positioned along a value axis",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with product data
        ChartData chartData = new()
        {
            Title = "Product Sales",
            Series =
            [
                new ChartSeries
                {
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Prod A", Value = 4500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod B", Value = 6200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod C", Value = 5800, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod D", Value = 7200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod E", Value = 8500, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod F", Value = 9100, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod G", Value = 8200, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Prod H", Value = 9500, LabelFormat = "N0" }
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 12000,
                StepCount = 5,
                Legend = "Sales"
            },
            XAxis = new AxisOptions
            {
                Legend = "Product"
            }
        };

        // Create the dot plot chart
        DotPlotChart dotPlotChart = new(chartData)
        {
            AltText = "Dot plot chart showing product sales performance across eight products",
            Height = 400,
            StretchToFit = true,
            DotColor = Color.FromHex(ChartColors.Teal),
            DotRadius = 12,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(dotPlotChart);

        // Save the document
        

        return document;
    }
}


