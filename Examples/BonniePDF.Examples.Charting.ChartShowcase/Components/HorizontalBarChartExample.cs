using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Demonstrates a horizontal bar chart - a good choice when category labels are long
/// or when ranking data reads more naturally left-to-right.
/// The only change from a standard BarChart is Orientation = ChartOrientation.Horizontal.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Bar Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("BarChart with Orientation = ChartOrientation.Horizontal - regional sales ranking where long category labels sit naturally on the left axis.")]
public static class HorizontalBarChartExample
{
    public static Document Generate()
    {
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;
        document.Settings.Language = "en-GB";
        document.Metadata.Title = "Horizontal Bar Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Regional sales ranking using a horizontal BarChart";

        document.Add(new TextBlock
        {
            Text = "Regional Sales Performance",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 10));

        document.Add(new TextBlock
        {
            Text = "Full-year revenue by region - horizontal orientation suits ranked comparisons",
            FontSize = 13,
            Color = ColorPalette.Slate[600],
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
                        new ChartDataPoint { Label = "North East",  Value = 142000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "North West",  Value = 187000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Midlands",    Value = 203000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "East Anglia", Value = 96000,  LabelFormat = "N0" },
                        new ChartDataPoint { Label = "South East",  Value = 261000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "South West",  Value = 118000, LabelFormat = "N0" },
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

        BarChart barChart = new(chartData)
        {
            AltText = "Horizontal bar chart ranking regional sales performance across six UK regions",
            Orientation = ChartOrientation.Horizontal,
            Height = 380,
            StretchToFit = true,
            BarColor = Color.FromHex(ChartColors.Indigo),
            BarWidthRatio = 0.55f,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        document.Add(barChart);
        

        //string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        //File.WriteAllText(svgChartPath, barChart.ToSvg(chartData));

        //string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        //File.WriteAllText(svgPagePath, document.Pages[0].ToSvg());
        return document;
    }
}


