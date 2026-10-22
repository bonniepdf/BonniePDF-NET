using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

/// <summary>
/// Demonstrates a horizontal bar chart - a good choice when category labels are long
/// or when ranking data reads more naturally left-to-right.
/// The only change from a standard BarChart is Orientation = ChartOrientation.Horizontal.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Bar Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "BarChart with Orientation = ChartOrientation.Horizontal - regional sales ranking where long category labels sit naturally on the left axis.")]
public static class HorizontalBarChartExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack footer = new() { WidthDistribution = WidthDistribution.Weighted };
                footer.Add(new TextBlock { Text = "BonniePDF - Horizontal Bar Chart", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500] });
                footer.Add(new TextBlock { Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500], Alignment = TextAlignment.Right }, 1f);
                return footer;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);
        document.Metadata.Title = "Horizontal Bar Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Regional sales ranking using a horizontal BarChart";

        document.Add(new TextBlock
        {
            Text = "Horizontal Bar Chart: Regional Sales Performance",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Full-year revenue by region - horizontal orientation suits ranked comparisons",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        ChartData chartData = new()
        {
            Title = "Regional Sales",
            Series =
            [
                new ChartSeries
                {
                    Color = Color.FromHex("#4f46e5"),
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "North East", Value = 142000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "North West", Value = 187000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Midlands", Value = 203000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "East Anglia", Value = 96000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "South East", Value = 261000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "South West", Value = 118000, LabelFormat = "N0" },
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
            BarColor = Color.FromHex("#4f46e5"),
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

    // Local helpers and sample data for this document.


}
