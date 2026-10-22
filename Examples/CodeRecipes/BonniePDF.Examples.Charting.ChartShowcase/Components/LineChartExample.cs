using System.Globalization;
using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.Charting.ChartShowcase.Components;

/// <summary>
/// Plot monthly revenue with a line chart, labelled points, and a currency axis.
/// The sample data is included below the document-building method.
/// </summary>
[System.ComponentModel.DisplayName("Line Chart")]
[System.ComponentModel.Category("Charting")]
[System.ComponentModel.Description(
    "LineChart implementation with monthly revenue trends, showing connected data points with customizable line width and markers.")]
public static class LineChartExample
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
                        Text = "BonniePDF - Line Chart", FontName = StandardFonts.Helvetica, FontSize = 10,
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
        document.Metadata.Title = "Line Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the LineChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Line Chart: Monthly Revenue Trends",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Connected data points with markers and value labels",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Revenue",
            Series = [new ChartSeries { Color = Color.FromHex("#7c3aed"), DataPoints = MonthlySales }],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create the line chart
        LineChart lineChart = new(chartData)
        {
            AltText = "Line chart showing monthly revenue trends from January to December",
            Height = 380,
            StretchToFit = true,
            LineColor = Color.FromHex("#7c3aed"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add the chart to the document
        document.Add(lineChart);

        // Save the document


        return document;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static List<string> MonthLabels => new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private static IList<ChartDataPoint> MonthlySales => new List<ChartDataPoint>
    {
        new ChartDataPoint
        {
            Label = MonthLabels[0],
            Value = 4500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[1],
            Value = 6200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[2],
            Value = 5800,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[3],
            Value = 7200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[4],
            Value = 8500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[5],
            Value = 9100,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[6],
            Value = 8200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[7],
            Value = 7800,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[8],
            Value = 9500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[9],
            Value = 10200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[10],
            Value = 11500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = MonthLabels[11],
            Value = 12800,
            LabelFormat = "N0"
        }
    };
}
