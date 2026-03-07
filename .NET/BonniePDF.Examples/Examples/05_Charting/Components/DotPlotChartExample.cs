using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._05_Charting.Components;

/// <summary>
/// Example demonstrating the DotPlotChart implementation.
/// This shows how a DotPlotChart can be rendered to PDF using the ICanvas interface.
/// </summary>
public class DotPlotChartExample : ExampleBase
{
    public override string Id => "dot-plot-chart";
    public override string Title => "Dot Plot Chart";
    public override string Description => "Demonstrates the DotPlotChart implementation with product performance data, showing dots at data points with customizable radius and colors.";
    public override string FileName => "dot_plot_chart_example.pdf";
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
        document.Metadata.Title = "Dot Plot Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the DotPlotChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Product Performance Analysis",
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
                        new ChartDataPoint { Label = "Prod A", Value = 4500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod B", Value = 6200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod C", Value = 5800, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod D", Value = 7200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod E", Value = 8500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod F", Value = 9100, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod G", Value = 8200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Prod H", Value = 9500, LabelFormat = "'£'#,##0" }
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
        DotPlotChart dotPlotChart = new()
        {
            Height = 400,
            StretchToFit = true,
            DotColor = Color.FromHex("#4f46e5"),
            DotRadius = 12,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(new ChartElement(dotPlotChart, chartData));

        // Save the document
        document.Save(outputPath);

        // Export Chart to SVG
        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        string svgChart = dotPlotChart.ToSvg(chartData);
        File.WriteAllText(svgChartPath, svgChart);

        // Export Page to SVG
        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        string svgPage = document.Pages[0].ToSvg();
        File.WriteAllText(svgPagePath, svgPage);

        return outputPath;
    }
}