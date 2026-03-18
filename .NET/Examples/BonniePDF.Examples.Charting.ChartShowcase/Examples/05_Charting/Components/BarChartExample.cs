using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Examples._05_Charting.Components;

/// <summary>
/// Example demonstrating the new refactored chart architecture.
/// This shows how a BarChart can be rendered to PDF using the ICanvas interface.
/// </summary>
public static class BarChartExample
{

    public static string Generate(string outputPath)
    {
        // Create a new PDF document
        using Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;

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
            Title = "Monthly Sales",
            Series =
            [
                new ChartSeries
                {
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Jan", Value = 4500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Feb", Value = 6200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Mar", Value = 5800, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Apr", Value = 7200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "May", Value = 8500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Jun", Value = 9100, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Jul", Value = 8200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Aug", Value = 7800, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Sep", Value = 9500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Oct", Value = 10200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Nov", Value = 11500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Dec", Value = 12800, LabelFormat = "'£'#,##0" }
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
        BarChart barChart = new()
        {
            Height = 400,
            StretchToFit = true,
            BarColor = Color.FromHex("#4f46e5"),
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Get the current page's content stream and page height
        // We need to use the PdfCanvas to draw the chart
        // For this example, we'll create a custom element that wraps the chart

        document.Add(new ChartElement(barChart, chartData));

        // Save the document
        document.Save(outputPath);

        // Export Chart to SVG
        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        string svgChart = barChart.ToSvg(chartData);
        File.WriteAllText(svgChartPath, svgChart);

        // Export Page to SVG
        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        string svgPage = document.Pages[0].ToSvg();
        File.WriteAllText(svgPagePath, svgPage);

        return outputPath;
    }
}
