using System.IO;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Example demonstrating the AreaChart implementation.
/// This shows how an AreaChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Area Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description("AreaChart implementation with monthly sales performance data, showing filled areas under the line with customizable opacity.")]
public static class AreaChartExample
{

    public static Document Generate()
    {
        // Create a new PDF document
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack stack = new() { WidthDistribution = WidthDistribution.Weighted };
                stack.Add(new TextBlock { Text = "BonniePDF - Area Chart", FontName = StandardFonts.Helvetica, FontSize = 10, TextColor = ColorPalette.Slate[500] }, null);
                stack.Add(new TextBlock { Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", FontName = StandardFonts.Helvetica, FontSize = 10, TextColor = ColorPalette.Slate[500], Alignment = TextAlignment.Right }, 1f);
                return stack;
            }
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
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Filled area under the line with configurable opacity",
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
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
                StepCount = 5,
                Legend = "Sales",
                VerticalLegend = true
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create the area chart
        AreaChart areaChart = new(chartData)
        {
            AltText = "Area chart showing monthly sales performance from January to December",
            Height = 400,
            StretchToFit = true,
            AreaColor = Color.FromHex(ChartColors.Purple),
            FillOpacity = 0.3f,
            LineWidth = 2.5f,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(areaChart);

        // Save the document
        

        return document;
    }
}


