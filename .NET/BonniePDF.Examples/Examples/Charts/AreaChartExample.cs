using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Examples.Charts;

/// <summary>
/// Example demonstrating the AreaChart implementation.
/// This shows how an AreaChart can be rendered to PDF using the ICanvas interface.
/// </summary>
public class AreaChartExample
{
    public static string Generate(string outputPath = "area_chart_example.pdf")
    {
        // Create a new PDF document
        using Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;

        // Set document metadata
        document.Metadata.Title = "Area Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the AreaChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Monthly Sales Performance",
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
            DataPoints =
            {
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
            },
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5,
                Legend = "Sales"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create the area chart
        AreaChart areaChart = new()
        {
            Height = 400,
            StretchToFit = true,
            AreaColor = Color.FromHex("#4f46e5"),
            FillOpacity = 0.3f,
            LineWidth = 2.5f,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(new ChartElement(areaChart, chartData));

        // Save the document
        document.Save(outputPath);

        // Export Chart to SVG
        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        string svgChart = areaChart.ToSvg(chartData);
        File.WriteAllText(svgChartPath, svgChart);

        // Export Page to SVG
        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        string svgPage = document.Pages[0].ToSvg();
        File.WriteAllText(svgPagePath, svgPage);

        return outputPath;
    }
}