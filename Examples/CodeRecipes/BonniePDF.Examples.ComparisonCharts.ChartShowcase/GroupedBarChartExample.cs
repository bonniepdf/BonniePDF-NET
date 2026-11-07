using System.Collections.Generic;
using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

/// <summary>
/// Example demonstrating the GroupedBarChart implementation.
/// This shows how a GroupedBarChart can be rendered to PDF using the ICanvas interface.
/// </summary>
[System.ComponentModel.DisplayName("Grouped Bar Chart")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "GroupedBarChart implementation with quarterly revenue data by product line, showing multiple bars per category with value labels.")]
public static class GroupedBarChartExample
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
                HorizontalStack footer = new() { WidthDistribution = WidthDistribution.Weighted };
                footer.Add(new TextBlock { Text = "BonniePDF - Grouped Bar Chart", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500] });
                footer.Add(new TextBlock { Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", FontName = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[500], Alignment = TextAlignment.Right }, 1f);
                return footer;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        document.Metadata.Title = "Grouped Bar Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the GroupedBarChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Grouped Bar Chart: Quarterly Revenue by Product Line",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        document.Add(new TextBlock
        {
            Text = "Three product lines compared quarter by quarter, 2026",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

        List<string> quarters = ["Q1", "Q2", "Q3", "Q4"];
        decimal[] electronicsValues = [14500, 21800, 22500, 31500];
        decimal[] clothingValues = [10500, 14200, 14100, 19400];
        decimal[] homeValues = [9300, 13000, 12900, 17400];

        ChartSeries electronicsSeries = new() { Name = "Electronics", Color = Color.FromHex("#4f46e5") };
        ChartSeries clothingSeries = new() { Name = "Clothing", Color = Color.FromHex("#f43f5e") };
        ChartSeries homeSeries = new() { Name = "Home & Garden", Color = Color.FromHex("#22c55e") };

        for (int i = 0; i < quarters.Count; i++)
        {
            electronicsSeries.DataPoints.Add(new ChartDataPoint { Label = quarters[i], Value = electronicsValues[i], LabelFormat = "N0" });
            clothingSeries.DataPoints.Add(new ChartDataPoint { Label = quarters[i], Value = clothingValues[i], LabelFormat = "N0" });
            homeSeries.DataPoints.Add(new ChartDataPoint { Label = quarters[i], Value = homeValues[i], LabelFormat = "N0" });
        }

        ChartData chartData = new()
        {
            YAxis = { MaxValue = 35000, Prefix = "£", StepCount = 7, ShowGridLines = true }
        };
        chartData.Series.Add(electronicsSeries);
        chartData.Series.Add(clothingSeries);
        chartData.Series.Add(homeSeries);

        // Create the grouped bar chart with 3 series (3 bars per quarter)
        GroupedBarChart groupedBarChart = new(chartData)
        {
            AltText =
                "Grouped bar chart comparing Electronics, Clothing, and Home and Garden revenue in each quarter of 2026; Electronics leads every quarter, reaching 31,500 GBP in Q4.",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            ShowValueLabels = true,
            Theme = "modern",
            BackgroundColor = Color.White,
        };

        // Add the chart to the document
        document.Add(groupedBarChart);

        // Save the document


        return document;
    }
}
