using System;
using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Charting.ChartShowcase.Data;
using BonniePDF.Extensions;

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
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;

        // Set document metadata
        document.Metadata.Title = "Pie Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the PieChart component";

        // Add a title
        document.Add(new TextBlock
        {
            Text = $"Monthly Sales Report {DateTime.Now.Year}",
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using the PieChart component with donut style",
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months (matching the SVG data)
        ChartData chartData = new()
        {
            Title = $"Monthly Sales {DateTime.Now.Year}",
            Series = [new ChartSeries { DataPoints = ExampleChartData.PieChartSales }]
        };

        // Create the pie chart with custom colors matching the SVG
        PieChart pieChart = new()
        {
            Height = 400,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total",
            ShowLegend = true,
            Colors = ChartColors.All,
            BackgroundColor = Color.White
        };

        // Add the chart to the document
        document.Add(new ChartElement(pieChart, chartData));

        // Save the document
        

        return document;
    }
}


