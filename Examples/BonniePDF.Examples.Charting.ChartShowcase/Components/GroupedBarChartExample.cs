using System.Collections.Generic;
using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Example demonstrating the GroupedBarChart implementation.
/// This shows how a GroupedBarChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Grouped Bar Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("GroupedBarChart implementation with quarterly revenue data by product line, showing multiple bars per category with value labels.")]
public static class GroupedBarChartExample
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
        document.Metadata.Title = "Grouped Bar Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the GroupedBarChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Quarterly Revenue by Product Line",
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Comparison of three product lines across all months",
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        List<string> months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        decimal[] electronicsValues = [4500, 5200, 4800, 6200, 7500, 8100, 7200, 6800, 8500, 9200, 10500, 11800];
        decimal[] clothingValues = [3200, 3800, 3500, 4200, 4800, 5200, 4600, 4400, 5100, 5800, 6400, 7200];
        decimal[] homeValues = [2800, 3400, 3100, 3800, 4400, 4800, 4200, 4000, 4700, 5400, 6000, 6800];

        ChartSeries electronicsSeries = new() { Name = "Electronics", Color = ColorPalette.Indigo[600] };
        ChartSeries clothingSeries = new() { Name = "Clothing", Color = ColorPalette.Rose[600] };
        ChartSeries homeSeries = new() { Name = "Home & Garden", Color = ColorPalette.Emerald[600] };

        for (int i = 0; i < months.Count; i++)
        {
            electronicsSeries.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = electronicsValues[i] });
            clothingSeries.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = clothingValues[i] });
            homeSeries.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = homeValues[i] });
        }

        ChartData chartData = new()
        {
            YAxis = { MaxValue = 15000, Prefix = "£", StepCount = 5, ShowGridLines = true }
        };
        chartData.Series.Add(electronicsSeries);
        chartData.Series.Add(clothingSeries);
        chartData.Series.Add(homeSeries);

        // Create the grouped bar chart with 3 series (3 bars per month)
        GroupedBarChart groupedBarChart = new()
        {
            Height = 400,
            StretchToFit = true,
            FontFamily = "Arial, sans-serif",
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa"),
        };

        // Add the chart to the document
        document.Add(new GroupedBarChartElement(groupedBarChart, chartData));

        // Save the document
        

        return document;
    }
}


