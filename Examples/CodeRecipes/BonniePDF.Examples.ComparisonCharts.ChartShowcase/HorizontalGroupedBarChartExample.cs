using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

/// <summary>
/// Demonstrates a horizontal grouped bar chart with multiple series.
/// Horizontal orientation works particularly well for grouped data when category names
/// are long, since the left-side axis gives generous space for labels.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Grouped Bar Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "GroupedBarChart with Orientation = ChartOrientation.Horizontal - quarterly sales for three product lines shown as horizontal grouped bars.")]
public static class HorizontalGroupedBarChartExample
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
                footer.Add(new TextBlock { Text = "BonniePDF - Horizontal Grouped Bar Chart", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500] });
                footer.Add(PageNumber.Text().Font(StandardFonts.Helvetica, 10).Color(ColorPalette.Slate[500]).AlignRight(), 1f);
                return footer;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);
        document.Metadata.Title = "Horizontal Grouped Bar Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Quarterly regional sales using a horizontal GroupedBarChart";

        document.Add(new TextBlock
        {
            Text = "Horizontal Grouped Bar Chart: Quarterly Sales by Region",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Three product lines compared across four quarters - horizontal grouped layout",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        ChartSeries electronicsSeries = new()
        {
            Name = "Electronics",
            Color = Color.FromHex("#4f46e5"),
            DataPoints =
            [
                new ChartDataPoint { Label = "Q1", Value = 42000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q2", Value = 58000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q3", Value = 71000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q4", Value = 95000, LabelFormat = "N0" },
            ]
        };

        ChartSeries clothingSeries = new()
        {
            Name = "Clothing",
            Color = Color.FromHex("#e11d48"),
            DataPoints =
            [
                new ChartDataPoint { Label = "Q1", Value = 31000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q2", Value = 44000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q3", Value = 52000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q4", Value = 68000, LabelFormat = "N0" },
            ]
        };

        ChartSeries homeSeries = new()
        {
            Name = "Home & Garden",
            Color = Color.FromHex("#16a34a"),
            DataPoints =
            [
                new ChartDataPoint { Label = "Q1", Value = 24000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q2", Value = 37000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q3", Value = 41000, LabelFormat = "N0" },
                new ChartDataPoint { Label = "Q4", Value = 53000, LabelFormat = "N0" },
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

        GroupedBarChart groupedBarChart = new(chartData)
        {
            AltText =
                "Horizontal grouped bar chart comparing quarterly sales across Electronics, Clothing, and Home & Garden",
            Orientation = ChartOrientation.Horizontal,
            Height = 380,
            StretchToFit = true,
            BarWidthRatio = 0.7f,
            GroupGapRatio = 0.25f,
            FontFamily = StandardFonts.Helvetica,
            ShowValueLabels = true, // keep it uncluttered with 3 series
            ShowLegend = true,
            Theme = "modern",
            BackgroundColor = Color.White
        };

        document.Add(groupedBarChart);


        //string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        //File.WriteAllText(svgChartPath, groupedBarChart.ToSvg(chartData));

        //string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        //File.WriteAllText(svgPagePath, document.Pages[0].ToSvg());
        return document;
    }

    // Local helpers and sample data for this document.


}
