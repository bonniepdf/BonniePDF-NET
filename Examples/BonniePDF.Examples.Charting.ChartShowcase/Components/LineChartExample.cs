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
/// Example demonstrating the LineChart implementation.
/// This shows how a LineChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Line Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("LineChart implementation with monthly revenue trends, showing connected data points with customizable line width and markers.")]
public static class LineChartExample
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
        document.Metadata.Title = "Line Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the LineChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Monthly Revenue Trends",
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using the new ICanvas architecture",
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Revenue",
            Series = [new ChartSeries { DataPoints = ExampleChartData.MonthlySales }],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create the line chart
        LineChart lineChart = new()
        {
            Height = 400,
            StretchToFit = true,
            LineColor = Color.FromHex("#4f46e5"),
            LineWidth = 2.5f,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            ShowDataPointMarkers = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(new ChartElement(lineChart, chartData));

        // Save the document
        

        return document;
    }
}


