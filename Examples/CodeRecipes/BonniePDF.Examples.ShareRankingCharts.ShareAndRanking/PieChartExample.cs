using System.Globalization;
using System;
using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.ShareRankingCharts.ShareAndRanking;

/// <summary>
/// Example demonstrating the PieChart component.
/// This shows how a PieChart can be rendered to PDF using the ICanvas interface.
/// Uses the same data and colors from the sales-piechart.svg file.
/// </summary>
[System.ComponentModel.DisplayName("Pie Chart")]
[System.ComponentModel.Category("Share & Ranking Charts")]
[System.ComponentModel.Description(
    "PieChart component with monthly sales data, showing a donut-style pie chart with customizable colors and legend.")]
public static class PieChartExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        // Create a new PDF document
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack stack = new() { WidthDistribution = WidthDistribution.Weighted };
                stack.Add(
                    new TextBlock
                    {
                        Text = "BonniePDF - Pie Chart", FontName = StandardFonts.Helvetica, FontSize = 10,
                        Color = ColorPalette.Slate[500]
                    }, null);
                stack.Add(
                    new TextBlock
                    {
                        Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                        FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500],
                        Alignment = TextAlignment.Right
                    }, 1f);
                return stack;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        document.Metadata.Title = "Pie Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the PieChart component";

        // Add a title
        document.Add(new TextBlock
        {
            Text = $"Pie Chart: Monthly Sales {DateTime.Now.Year}",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Donut chart with legend and total value in the centre",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        // Create chart data with all 12 months (matching the SVG data)
        ChartData chartData = new()
        {
            Title = $"Monthly Sales {DateTime.Now.Year}",
            Series = [new ChartSeries { DataPoints = PieChartSales }]
        };

        // Create the pie chart with custom colors matching the SVG
        PieChart pieChart = new(chartData)
        {
            AltText = "Donut-style pie chart showing proportional distribution of monthly sales",
            Height = 380,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total",
            ShowLegend = true,
            Colors = new[] { "#4f46e5", "#7c3aed", "#a855f7", "#d946ef", "#ec4899", "#f43f5e", "#f97316", "#eab308", "#84cc16", "#22c55e", "#14b8a6", "#06b6d4" },
            BackgroundColor = Color.White
        };

        // Add the chart to the document
        document.Add(pieChart);

        // Save the document


        return document;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static List<string> MonthLabels => new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private static IList<ChartDataPoint> PieChartSales => new List<ChartDataPoint>()
    {
        new ChartDataPoint
        {
            Label = MonthLabels[0],
            Value = 7234,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[1],
            Value = 4891,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[2],
            Value = 8456,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[3],
            Value = 3127,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[4],
            Value = 9542,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[5],
            Value = 6789,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[6],
            Value = 5234,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[7],
            Value = 8901,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[8],
            Value = 2456,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[9],
            Value = 7890,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[10],
            Value = 6123,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[11],
            Value = 9876,
            LabelFormat = "N0"
        }
    };
}
