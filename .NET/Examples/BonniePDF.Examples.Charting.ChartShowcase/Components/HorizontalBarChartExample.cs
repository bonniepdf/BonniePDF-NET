using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Demonstrates a horizontal bar chart — a good choice when category labels are long
/// or when ranking data reads more naturally left-to-right.
/// The only change from a standard BarChart is Orientation = ChartOrientation.Horizontal.
/// </summary>
public static class HorizontalBarChartExample
{
    public static string Generate(string outputPath)
    {
        using Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;
        document.Metadata.Title = "Horizontal Bar Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Regional sales ranking using a horizontal BarChart";

        document.Add(new TextBlock
        {
            Text = "Regional Sales Performance",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 10));

        document.Add(new TextBlock
        {
            Text = "Full-year revenue by region — horizontal orientation suits ranked comparisons",
            FontSize = 13,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        ChartData chartData = new()
        {
            Title = "Regional Sales",
            Series =
            [
                new ChartSeries
                {
                    Color = Color.FromHex(ChartColors.Indigo),
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "North East",  Value = 142000, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "North West",  Value = 187000, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Midlands",    Value = 203000, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "East Anglia", Value = 96000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "South East",  Value = 261000, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "South West",  Value = 118000, LabelFormat = "'£'#,##0" },
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 300000,
                StepCount = 5,
                ShowGridLines = true,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions { Legend = "Region" }
        };

        BarChart barChart = new()
        {
            Orientation = ChartOrientation.Horizontal,
            Height = 380,
            StretchToFit = true,
            BarColor = Color.FromHex(ChartColors.Indigo),
            BarWidthRatio = 0.55f,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        document.Add(new ChartElement(barChart, chartData));
        document.Save(outputPath);

        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        File.WriteAllText(svgChartPath, barChart.ToSvg(chartData));

        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        File.WriteAllText(svgPagePath, document.Pages[0].ToSvg());

        return outputPath;
    }
}
