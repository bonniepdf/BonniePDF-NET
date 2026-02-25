using System.Collections.Generic;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Data;
using BonniePDF.Examples.Examples._05_Charting.Components;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._05_Charting;

/// <summary>
/// A comprehensive example that generates a multipage PDF containing all chart types.
/// </summary>
public class ChartingExample
{
    public static string Generate(string outputPath = "charting_example.pdf")
    {
        // Create a new PDF document
        using Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack outerStack = new()
                {
                    WidthDistribution = WidthDistribution.Weighted,
                    Spacing = 0,
                    Alignment = HorizontalAlignment.Center // Vertically align items
                };

                // Left: Title (auto-width, left-aligned)
                outerStack.AddChild(new TextBlock
                {
                    Text = "BonniePDF Charting Examples",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    TextColor = Color.Slate500
                }, null); // Auto width

                // Right: Page Numbers (weighted to fill remaining space, right-aligned within that space)
                outerStack.AddChild(new TextBlock
                {
                    Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    TextColor = Color.Slate500,
                    Alignment = TextAlignment.Right
                }, 1f); // Takes all remaining space and right-aligns within it

                return outerStack;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.PdfAEnabled = true;

        // Set document metadata
        document.Metadata.Title = "Charting Examples";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Charting Examples demonstrating all chart types";

        // 1. Bar Chart (on first page)
        AddBarChart(document);

        // 2. Grouped Bar Chart (on second page)
        document.AddPage();
        AddGroupedBarChart(document);

        // 3. Line Chart (on third page)
        document.AddPage();
        AddLineChart(document);

        // 4. Area Chart (on fourth page)
        document.AddPage();
        AddAreaChart(document);

        // 5. Lollipop Chart (on fifth page)
        document.AddPage();
        AddLollipopChart(document);

        // 6. Dot Plot Chart (on sixth page)
        document.AddPage();
        AddDotPlotChart(document);

        // 7. Pie Chart (on seventh page)
        document.AddPage();
        AddPieChart(document);

        // 8. Multi-Series Line Chart (on eighth page)
        document.AddPage();
        AddMultiSeriesLineChart(document);

        // Save document
        document.Save(outputPath);

        return outputPath;
    }

    private static void AddBarChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Bar Chart: Monthly Sales Report",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales",
            DataPoints = ExampleChartData.MonthlySales,
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5
            }
        };

        // Create bar chart
        BarChart barChart = new()
        {
            Height = 400,
            StretchToFit = true,
            BarColor = Color.FromHex(ChartColors.Indigo),
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(new ChartElement(barChart, chartData));
    }

    private static void AddGroupedBarChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Grouped Bar Chart: Quarterly Revenue",
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

        // Create grouped bar chart with 3 series
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
                    Color = Color.FromHex(ChartColors.Indigo),
                    Values = new List<decimal>
                    {
                        4500, 5200, 4800, 6200, 7500, 8100, 7200, 6800, 8500, 9200, 10500, 11800
                    }
                },
                new()
                {
                    Name = "Clothing",
                    Color = Color.FromHex(ChartColors.Rose),
                    Values = new List<decimal>
                    {
                        3200, 3800, 3500, 4200, 4800, 5200, 4600, 4400, 5100, 5800, 6400, 7200
                    }
                },
                new()
                {
                    Name = "Home & Garden",
                    Color = Color.FromHex(ChartColors.Green),
                    Values = new List<decimal>
                    {
                        2800, 3400, 3100, 3800, 4400, 4800, 4200, 4000, 4700, 5400, 6000, 6800
                    }
                }
            },

            // Category labels for all 12 months
            CategoryLabels = ExampleChartData.MonthLabels
        };

        // Add chart to document
        document.Add(new GroupedBarChartElement(groupedBarChart));
    }

    private static void AddLineChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Line Chart: Monthly Revenue Trends",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Revenue",
            DataPoints = ExampleChartData.MonthlySales,
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

        // Create line chart
        LineChart lineChart = new()
        {
            Height = 400,
            StretchToFit = true,
            LineColor = Color.FromHex(ChartColors.Violet),
            LineWidth = 2.5f,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            ShowDataPointMarkers = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(new ChartElement(lineChart, chartData));
    }

    private static void AddMultiSeriesLineChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Multi-Series Line Chart: Year-over-Year Comparison",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Comparing 2023 and 2024 revenue with multiple series",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with multiple series
        ChartData chartData = new()
        {
            Title = "Year-over-Year Revenue Comparison",
            Series = new List<ChartSeries>
            {
                new ChartSeries
                {
                    Name = "2023",
                    Color = Color.FromHex(ChartColors.Indigo),
                    DataPoints = new List<ChartDataPoint>
                    {
                        new ChartDataPoint { Label = "Jan", Value = 65000 },
                        new ChartDataPoint { Label = "Feb", Value = 69000 },
                        new ChartDataPoint { Label = "Mar", Value = 80000 },
                        new ChartDataPoint { Label = "Apr", Value = 72000 },
                        new ChartDataPoint { Label = "May", Value = 85000 },
                        new ChartDataPoint { Label = "Jun", Value = 88000 },
                        new ChartDataPoint { Label = "Jul", Value = 72000 },
                        new ChartDataPoint { Label = "Aug", Value = 68000 },
                        new ChartDataPoint { Label = "Sep", Value = 75000 },
                        new ChartDataPoint { Label = "Oct", Value = 81000 },
                        new ChartDataPoint { Label = "Nov", Value = 92000 },
                        new ChartDataPoint { Label = "Dec", Value = 105000 }
                    }
                },
                new ChartSeries
                {
                    Name = "2024",
                    Color = Color.FromHex(ChartColors.Violet),
                    DataPoints = new List<ChartDataPoint>
                    {
                        new ChartDataPoint { Label = "Jan", Value = 75000 },
                        new ChartDataPoint { Label = "Feb", Value = 78000 },
                        new ChartDataPoint { Label = "Mar", Value = 85000 },
                        new ChartDataPoint { Label = "Apr", Value = 78000 },
                        new ChartDataPoint { Label = "May", Value = 92000 },
                        new ChartDataPoint { Label = "Jun", Value = 95000 },
                        new ChartDataPoint { Label = "Jul", Value = 87000 },
                        new ChartDataPoint { Label = "Aug", Value = 78000 },
                        new ChartDataPoint { Label = "Sep", Value = 82000 },
                        new ChartDataPoint { Label = "Oct", Value = 91000 },
                        new ChartDataPoint { Label = "Nov", Value = 97000 },
                        new ChartDataPoint { Label = "Dec", Value = 118000 }
                    }
                }
            },
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 150000,
                StepCount = 5,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create line chart with multiple series
        LineChart lineChart = new()
        {
            Height = 400,
            StretchToFit = true,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            ShowDataPointMarkers = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(new ChartElement(lineChart, chartData));
    }

    private static void AddAreaChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Area Chart: Monthly Sales Performance",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales",
            DataPoints = ExampleChartData.MonthlySales,
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5,
                Legend = "Sales"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        // Create area chart
        AreaChart areaChart = new()
        {
            Height = 400,
            StretchToFit = true,
            AreaColor = Color.FromHex(ChartColors.Purple),
            FillOpacity = 0.3f,
            LineWidth = 2.5f,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(new ChartElement(areaChart, chartData));
    }

    private static void AddLollipopChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Lollipop Chart: Quarterly Metrics",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with quarterly data
        ChartData chartData = new()
        {
            Title = "Quarterly Revenue",
            DataPoints = ExampleChartData.QuarterlyRevenue,
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

        // Create lollipop chart
        LollipopChart lollipopChart = new()
        {
            Height = 400,
            StretchToFit = true,
            LollipopColor = Color.FromHex(ChartColors.Pink),
            StemWidth = 2.5f,
            HeadRadius = 10,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(new ChartElement(lollipopChart, chartData));
    }

    private static void AddDotPlotChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Dot Plot Chart: Product Performance",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with product data
        ChartData chartData = new()
        {
            Title = "Product Sales",
            DataPoints = ExampleChartData.ProductSales,
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 12000,
                StepCount = 5,
                Legend = "Sales"
            },
            XAxis = new AxisOptions
            {
                Legend = "Product"
            }
        };

        // Create dot plot chart
        DotPlotChart dotPlotChart = new()
        {
            Height = 400,
            StretchToFit = true,
            DotColor = Color.FromHex(ChartColors.Teal),
            DotRadius = 12,
            FontFamily = "Arial, sans-serif",
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(new ChartElement(dotPlotChart, chartData));
    }

    private static void AddPieChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Pie Chart: Monthly Sales 2024",
            FontSize = 24,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        document.Add(new TextBlock
        {
            Text = "Generated using new ICanvas architecture",
            FontSize = 14,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
        }.Padding(0, 0, 0, 20));

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales 2024",
            DataPoints = ExampleChartData.PieChartSales
        };

        // Create pie chart
        PieChart pieChart = new()
        {
            Height = 400,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total",
            ShowLegend = true,
            Colors = ChartColors.All,
            BackgroundColor = Color.White
        };

        // Add chart to document
        document.Add(new ChartElement(pieChart, chartData));
    }
}
