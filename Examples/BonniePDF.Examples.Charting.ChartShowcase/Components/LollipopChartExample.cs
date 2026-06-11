using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Example demonstrating the LollipopChart implementation.
/// This shows how a LollipopChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Lollipop Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("LollipopChart implementation with quarterly performance metrics, showing data points as lollipops with customizable stem width and head radius.")]
public static class LollipopChartExample
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
        document.Metadata.Title = "Lollipop Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the LollipopChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Quarterly Performance Metrics",
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using the new ICanvas architecture",
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with quarterly data
        ChartData chartData = new()
        {
            Title = "Quarterly Revenue",
            Series =
            [
                new ChartSeries
                {
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Q1", Value = 4500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Q2", Value = 6200, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Q3", Value = 8500, LabelFormat = "'£'#,##0" },
                        new ChartDataPoint { Label = "Q4", Value = 11500, LabelFormat = "'£'#,##0" }
                    ]
                }
            ],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Quarter"
            }
        };

        // Create the lollipop chart
        LollipopChart lollipopChart = new()
        {
            Height = 400,
            StretchToFit = true,
            LollipopColor = Color.FromHex("#4f46e5"),
            StemWidth = 2.5f,
            HeadRadius = 10,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(new ChartElement(lollipopChart, chartData));

        // Save the document
        

        return document;
    }
}


