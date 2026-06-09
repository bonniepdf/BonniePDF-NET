using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Demonstrates a horizontal lollipop chart - an elegant alternative to horizontal bars
/// for ranking data where you want to reduce visual weight.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Lollipop Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("LollipopChart with Orientation = ChartOrientation.Horizontal - product revenue ranking with a lightweight stem-and-head style that reads cleanly left-to-right.")]
public static class HorizontalLollipopChartExample
{
    public static Document Generate()
    {
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;
        document.Metadata.Title = "Horizontal Lollipop Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Product revenue ranking using a horizontal LollipopChart";

        document.Add(new TextBlock
        {
            Text = "Product Revenue Ranking",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 10));

        document.Add(new TextBlock
        {
            Text = "Year-to-date revenue per product line - lollipop style reduces ink without losing clarity",
            FontSize = 13,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        ChartData chartData = new()
        {
            Title = "Product Revenue",
            Series =
            [
                new ChartSeries
                {
                    Color = Color.FromHex(ChartColors.Violet),
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Product A", Value = 45000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product B", Value = 62000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product C", Value = 38500,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product D", Value = 71000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product E", Value = 55000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product F", Value = 83000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product G", Value = 49000,  LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Product H", Value = 91000,  LabelFormat = "'£'#,##0" },
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 100000,
                StepCount = 5,
                ShowGridLines = true,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions { Legend = "Product" }
        };

        LollipopChart lollipopChart = new()
        {
            Orientation = ChartOrientation.Horizontal,
            Height = 400,
            StretchToFit = true,
            LollipopColor = Color.FromHex(ChartColors.Violet),
            StemWidth = 2f,
            HeadRadius = 9,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        document.Add(new ChartElement(lollipopChart, chartData));
        

        //string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        //File.WriteAllText(svgChartPath, lollipopChart.ToSvg(chartData));

        //string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        //File.WriteAllText(svgPagePath, document.Pages[0].ToSvg());
        return document;
    }
}


