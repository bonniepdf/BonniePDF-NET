using System;
using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Data;
using BonniePDF.Extensions;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._05_Charting.Components;

/// <summary>
/// Example demonstrating the PieChart component.
/// This shows how a PieChart can be rendered to PDF using the ICanvas interface.
/// Uses the same data and colors from the sales-piechart.svg file.
/// </summary>
public class PieChartExample : ExampleBase
{
    public override string Id => "pie-chart";
    public override string Title => "Pie Chart";
    public override string Description => "Demonstrates the PieChart component with monthly sales data, showing a donut-style pie chart with customizable colors and legend.";
    public override string FileName => "pie_chart_example.pdf";
    public override ExampleCategory Category => ExampleCategory.Charting;

    public override string Generate(string outputPath)
    {
        // Create a new PDF document
        using Document document = new(new DocumentSettings
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
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using the PieChart component with donut style",
            FontSize = 14,
            TextColor = Color.Slate600,
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
        document.Save(outputPath);

        // Export Chart to SVG
        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        string svgChart = pieChart.ToSvg(chartData);
        File.WriteAllText(svgChartPath, svgChart);

        // Export Page to SVG
        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        string svgPage = document.Pages[0].ToSvg();
        File.WriteAllText(svgPagePath, svgPage);

        return outputPath;
    }
}