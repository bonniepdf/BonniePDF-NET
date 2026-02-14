using System.Collections.Generic;
using System.IO;
using BonniePDF.Charts;
using BonniePDF.Configuration;
using BonniePDF.Extensions;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;

namespace BonniePDF.Examples.Examples.Charts;

/// <summary>
/// Example demonstrating the GroupedBarChart implementation.
/// This shows how a GroupedBarChart can be rendered to PDF using the ICanvas interface.
/// </summary>
public class GroupedBarChartExample
{
    public static string Generate(string outputPath = "grouped_bar_chart_example.pdf")
    {
        // Create a new PDF document
        using Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        });

        // Set document metadata
        document.Metadata.Title = "Grouped Bar Chart Example";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Demonstrating the GroupedBarChart with ICanvas architecture";

        // Add a title
        document.Add(new TextBlock
        {
            Text = "Quarterly Revenue by Product Line",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Comparison of three product lines across all months",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create the grouped bar chart with 3 series (3 bars per month)
        GroupedBarChart groupedBarChart = new()
        {
            Height = 400,
            StretchToFit = true,
            FontFamily = "Arial, sans-serif",
            ShowValueLabels = true,
            ShowGridLines = true,
            YAxisMaxValue = 15000,
            YAxisPrefix = "£",
            YAxisStepCount = 5,
            BackgroundColor = Color.FromHex("#fafafa"),

            // Define 3 series with different Tailwind colors
            Series = new List<BarSeries>
            {
                new()
                {
                    Name = "Electronics",
                    Color = Color.Indigo600, // Tailwind indigo-600
                    Values = new List<decimal>
                    {
                        4500, 5200, 4800, 6200, 7500, 8100, 7200, 6800, 8500, 9200, 10500, 11800
                    }
                },
                new()
                {
                    Name = "Clothing",
                    Color = Color.Rose600, // Tailwind rose-600
                    Values = new List<decimal>
                    {
                        3200, 3800, 3500, 4200, 4800, 5200, 4600, 4400, 5100, 5800, 6400, 7200
                    }
                },
                new()
                {
                    Name = "Home & Garden",
                    Color = Color.Emerald600, // Tailwind emerald-600
                    Values = new List<decimal>
                    {
                        2800, 3400, 3100, 3800, 4400, 4800, 4200, 4000, 4700, 5400, 6000, 6800
                    }
                }
            },

            // Category labels for all 12 months
            CategoryLabels = new List<string>
            {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            }
        };

        // Add the chart to the document
        document.Add(new GroupedBarChartElement(groupedBarChart));

        // Save the document
        document.Save(outputPath);

        // Export Chart to SVG
        string svgChartPath = outputPath.Replace(".pdf", "_chart.svg");
        // GroupedBarChart uses internal data, so we pass empty ChartData
        string svgChart = groupedBarChart.ToSvg(new ChartData());
        File.WriteAllText(svgChartPath, svgChart);

        // Export Page to SVG
        string svgPagePath = outputPath.Replace(".pdf", "_page.svg");
        string svgPage = document.Pages[0].ToSvg();
        File.WriteAllText(svgPagePath, svgPage);

        return outputPath;
    }
}