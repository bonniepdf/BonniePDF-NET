using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.ShareRankingCharts.ShareAndRanking;

/// <summary>
/// Demonstrates a horizontal lollipop chart - an elegant alternative to horizontal bars
/// for ranking data where you want to reduce visual weight.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Lollipop Chart")]
[System.ComponentModel.Category("Share & Ranking Charts")]
[System.ComponentModel.Description(
    "LollipopChart with Orientation = ChartOrientation.Horizontal - product revenue ranking with a lightweight stem-and-head style that reads cleanly left-to-right.")]
public static class HorizontalLollipopChartExample
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
                footer.Add(new TextBlock { Text = "BonniePDF - Horizontal Lollipop Chart", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500] });
                footer.Add(new TextBlock { Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500], Alignment = TextAlignment.Right }, 1f);
                return footer;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);
        document.Metadata.Title = "Horizontal Lollipop Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Product revenue ranking using a horizontal LollipopChart";

        document.Add(new TextBlock
        {
            Text = "Horizontal Lollipop Chart: Product Revenue Ranking",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Year-to-date revenue per product line - lollipop style reduces ink without losing clarity",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        ChartData chartData = new()
        {
            Title = "Product Revenue",
            Series =
            [
                new ChartSeries
                {
                    Color = Color.FromHex("#7c3aed"),
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Product H", Value = 91000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product F", Value = 83000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product D", Value = 71000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product B", Value = 62000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product E", Value = 55000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product G", Value = 49000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product A", Value = 45000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Product C", Value = 38500, LabelFormat = "N0" },
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

        LollipopChart lollipopChart = new(chartData)
        {
            AltText = "Horizontal lollipop chart ranking year-to-date revenue across eight product lines",
            Orientation = ChartOrientation.Horizontal,
            Height = 400,
            StretchToFit = true,
            LollipopColor = Color.FromHex("#7c3aed"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            Theme = "modern",
            BackgroundColor = Color.White
        };

        document.Add(lollipopChart);


        //string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        //File.WriteAllText(svgChartPath, lollipopChart.ToSvg(chartData));

        //string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        //File.WriteAllText(svgPagePath, document.Pages[0].ToSvg());
        return document;
    }

    // Local helpers and sample data for this document.


}
