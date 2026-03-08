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
/// Example demonstrating the LineChart implementation.
/// This shows how a LineChart can be rendered to PDF using the ICanvas interface.
/// </summary>
public class LineChartExample : ExampleBase
{
    public override string Id => "line-chart";
    public override string Title => "Line Chart";
    public override string Description => "LineChart implementation with monthly revenue trends, showing connected data points with customizable line width and markers.";
    public override string FileName => "line_chart_example.pdf";
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
        document.Metadata.Title = "Line Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the LineChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Monthly Revenue Trends",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using the new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
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
        document.Save(outputPath);

        // Export Chart to SVG
        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        string svgChart = lineChart.ToSvg(chartData);
        File.WriteAllText(svgChartPath, svgChart);

        // Export Page to SVG
        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        string svgPage = document.Pages[0].ToSvg();
        File.WriteAllText(svgPagePath, svgPage);

        return outputPath;
    }
}