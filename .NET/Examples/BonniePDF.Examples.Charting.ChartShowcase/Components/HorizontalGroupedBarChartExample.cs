using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Demonstrates a horizontal grouped bar chart with multiple series.
/// Horizontal orientation works particularly well for grouped data when category names
/// are long, since the left-side axis gives generous space for labels.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Grouped Bar Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("GroupedBarChart with Orientation = ChartOrientation.Horizontal — quarterly sales for three product lines shown as horizontal grouped bars.")]
public static class HorizontalGroupedBarChartExample
{
    public static string Generate(string outputPath)
    {
        using Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;
        document.Metadata.Title = "Horizontal Grouped Bar Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Quarterly regional sales using a horizontal GroupedBarChart";

        document.Add(new TextBlock
        {
            Text = "Quarterly Sales by Region",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 10));

        document.Add(new TextBlock
        {
            Text = "Three product lines compared across four quarters — horizontal grouped layout",
            FontSize = 13,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        ChartSeries electronicsSeries = new()
        {
            Name = "Electronics",
            Color = Color.FromHex(ChartColors.Indigo),
            DataPoints =
            [
                new ChartDataPoint { Label = "Q1", Value = 42000 },
                new ChartDataPoint { Label = "Q2", Value = 58000 },
                new ChartDataPoint { Label = "Q3", Value = 71000 },
                new ChartDataPoint { Label = "Q4", Value = 95000 },
            ]
        };

        ChartSeries clothingSeries = new()
        {
            Name = "Clothing",
            Color = Color.FromHex(ChartColors.Rose),
            DataPoints =
            [
                new ChartDataPoint { Label = "Q1", Value = 31000 },
                new ChartDataPoint { Label = "Q2", Value = 44000 },
                new ChartDataPoint { Label = "Q3", Value = 52000 },
                new ChartDataPoint { Label = "Q4", Value = 68000 },
            ]
        };

        ChartSeries homeSeries = new()
        {
            Name = "Home & Garden",
            Color = Color.FromHex(ChartColors.Green),
            DataPoints =
            [
                new ChartDataPoint { Label = "Q1", Value = 24000 },
                new ChartDataPoint { Label = "Q2", Value = 37000 },
                new ChartDataPoint { Label = "Q3", Value = 41000 },
                new ChartDataPoint { Label = "Q4", Value = 53000 },
            ]
        };

        ChartData chartData = new()
        {
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 110000,
                StepCount = 5,
                ShowGridLines = true,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions { Legend = "Quarter" }
        };
        chartData.Series.Add(electronicsSeries);
        chartData.Series.Add(clothingSeries);
        chartData.Series.Add(homeSeries);

        GroupedBarChart groupedBarChart = new()
        {
            Orientation = ChartOrientation.Horizontal,
            Height = 380,
            StretchToFit = true,
            BarWidthRatio = 0.7f,
            GroupGapRatio = 0.25f,
            FontFamily = "Arial, sans-serif",
            ShowValueLabels = false, // keep it uncluttered with 3 series
            ShowLegend = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        document.Add(new GroupedBarChartElement(groupedBarChart, chartData));
        document.Save(outputPath);

        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        File.WriteAllText(svgChartPath, groupedBarChart.ToSvg(chartData));

        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        File.WriteAllText(svgPagePath, document.Pages[0].ToSvg());

        return outputPath;
    }
}
