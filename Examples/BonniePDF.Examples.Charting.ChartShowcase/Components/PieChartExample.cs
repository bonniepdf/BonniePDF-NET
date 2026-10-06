using System;
using System.IO;
using BonniePDF;
using BonniePDF.Examples.Charting.ChartShowcase.Data;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Example demonstrating the PieChart component.
/// This shows how a PieChart can be rendered to PDF using the ICanvas interface.
/// Uses the same data and colors from the sales-piechart.svg file.
/// </summary>
[System.ComponentModel.DisplayName("Pie Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("PieChart component with monthly sales data, showing a donut-style pie chart with customizable colors and legend.")]
public static class PieChartExample
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
                stack.Add(new TextBlock { Text = "BonniePDF - Pie Chart", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500] }, null);
                stack.Add(new TextBlock { Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500], Alignment = TextAlignment.Right }, 1f);
                return stack;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;
        document.Settings.Language = "en-GB";

        // Set document metadata
        document.Metadata.Title = "Pie Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the PieChart component";

        // Add a title
        document.Add(new TextBlock
        {
            Text = $"Monthly Sales Report {DateTime.Now.Year}",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Donut chart with legend and total value in the centre",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months (matching the SVG data)
        ChartData chartData = new()
        {
            Title = $"Monthly Sales {DateTime.Now.Year}",
            Series = [new ChartSeries { DataPoints = ExampleChartData.PieChartSales }]
        };

        // Create the pie chart with custom colors matching the SVG
        PieChart pieChart = new(chartData)
        {
            AltText = "Donut-style pie chart showing proportional distribution of monthly sales",
            Height = 400,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total",
            ShowLegend = true,
            Colors = ChartColors.All,
            BackgroundColor = Color.White
        };

        // Add the chart to the document
        document.Add(pieChart);

        // Save the document
        

        return document;
    }
}


