using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

/// <summary>
/// A comprehensive example that generates a multipage PDF containing all chart types.
/// </summary>
[System.ComponentModel.DisplayName("Charting Examples")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "A comprehensive multi-page PDF showcasing all chart types including bar, grouped bar, line, area, lollipop, dot plot, and pie charts with various configurations.")]
public static class ChartShowcaseExample
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
                HorizontalStack outerStack = new()
                {
                    WidthDistribution = WidthDistribution.Weighted,
                    Spacing = 0,
                    Alignment = HorizontalAlignment.Center // Vertically align items
                };

                // Left: Title (auto-width, left-aligned)
                outerStack.Add(new TextBlock
                {
                    Text = "BonniePDF Charting Examples",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    Color = ColorPalette.Slate[500]
                }, null); // Auto width

                // Right: Page Numbers (weighted to fill remaining space, right-aligned within that space)
                outerStack.Add(new TextBlock
                {
                    Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    Color = ColorPalette.Slate[500],
                    Alignment = TextAlignment.Right
                }, 1f); // Takes all remaining space and right-aligns within it

                return outerStack;
            }
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle =
            new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        document.Metadata.Title = "Chart Examples";
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

        return document;
    }

    private static void AddBarChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Bar Chart: Monthly Sales Report",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Vertical bars with grid lines and value labels",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales",
            Series = [new ChartSeries { DataPoints = MonthlySales }],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 15000,
                StepCount = 5
            }
        };

        // Create bar chart
        BarChart barChart = new(chartData)
        {
            AltText = "Bar chart showing monthly sales from January to December with values in pounds sterling",
            Height = 400,
            StretchToFit = true,
            BarColor = Color.FromHex("#4f46e5"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(barChart);
    }

    private static void AddGroupedBarChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Grouped Bar Chart: Quarterly Revenue",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Comparison of three product lines across all months",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        List<string> months = MonthLabels;
        decimal[] electronicsValues = [4500, 5200, 4800, 6200, 7500, 8100, 7200, 6800, 8500, 9200, 10500, 11800];
        decimal[] clothingValues = [3200, 3800, 3500, 4200, 4800, 5200, 4600, 4400, 5100, 5800, 6400, 7200];
        decimal[] homeValues = [2800, 3400, 3100, 3800, 4400, 4800, 4200, 4000, 4700, 5400, 6000, 6800];

        ChartData groupedChartData = new()
        {
            YAxis =
            {
                MaxValue = 15000,
                Prefix = "£",
                StepCount = 5,
                ShowGridLines = true
            }
        };

        ChartSeries electronicsSeries = new() { Name = "Electronics", Color = Color.FromHex("#4f46e5") };
        ChartSeries clothingSeries = new() { Name = "Clothing", Color = Color.FromHex("#f43f5e") };
        ChartSeries homeSeries = new() { Name = "Home & Garden", Color = Color.FromHex("#22c55e") };

        for (int i = 0; i < months.Count; i++)
        {
            electronicsSeries.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = electronicsValues[i], LabelFormat = "N0" });
            clothingSeries.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = clothingValues[i], LabelFormat = "N0" });
            homeSeries.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = homeValues[i], LabelFormat = "N0" });
        }

        groupedChartData.Series.Add(electronicsSeries);
        groupedChartData.Series.Add(clothingSeries);
        groupedChartData.Series.Add(homeSeries);

        // Create grouped bar chart with 3 series
        GroupedBarChart groupedBarChart = new(groupedChartData)
        {
            AltText =
                "Grouped bar chart comparing monthly revenue across Electronics, Clothing, and Home & Garden product lines",
            Height = 400,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa"),
        };

        // Add chart to document
        document.Add(groupedBarChart);
    }

    private static void AddLineChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Line Chart: Monthly Revenue Trends",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Connected data points with markers and value labels",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

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

        // Create line chart
        LineChart lineChart = new(chartData)
        {
            AltText = "Line chart showing monthly revenue trends from January to December",
            Height = 400,
            StretchToFit = true,
            LineColor = Color.FromHex("#7c3aed"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(lineChart);
    }

    private static void AddMultiSeriesLineChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Multi-Series Line Chart: Year-over-Year Comparison",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = $"Comparing {DateTime.Now.Year - 1} and {DateTime.Now.Year} revenue with multiple series",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with multiple series
        ChartData chartData = new()
        {
            Title = "Year-over-Year Revenue Comparison",
            Series = new List<ChartSeries>
            {
                new ChartSeries
                {
                    Name = $"{DateTime.Now.Year - 1}",
                    Color = Color.FromHex("#4f46e5"),
                    DataPoints = new List<ChartDataPoint>
                    {
                        new ChartDataPoint { Label = "Jan", Value = 65000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Feb", Value = 69000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Mar", Value = 80000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Apr", Value = 72000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "May", Value = 85000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jun", Value = 88000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jul", Value = 72000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Aug", Value = 68000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Sep", Value = 75000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Oct", Value = 81000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Nov", Value = 92000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Dec", Value = 105000, LabelFormat = "N0" }
                    }
                },
                new ChartSeries
                {
                    Name = $"{DateTime.Now.Year}",
                    Color = Color.FromHex("#7c3aed"),
                    DataPoints = new List<ChartDataPoint>
                    {
                        new ChartDataPoint { Label = "Jan", Value = 75000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Feb", Value = 78000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Mar", Value = 85000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Apr", Value = 78000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "May", Value = 92000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jun", Value = 95000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Jul", Value = 87000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Aug", Value = 78000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Sep", Value = 82000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Oct", Value = 91000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Nov", Value = 97000, LabelFormat = "N0" },
                        new ChartDataPoint { Label = "Dec", Value = 118000, LabelFormat = "N0" }
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
        LineChart lineChart = new(chartData)
        {
            AltText = "Multi-series line chart comparing year-over-year monthly revenue",
            Height = 400,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(lineChart);
    }

    private static void AddAreaChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Area Chart: Monthly Sales Performance",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Filled area under the line with configurable opacity",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales",
            Series = [new ChartSeries { Color = Color.FromHex("#a855f7"), DataPoints = MonthlySales }],
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
        AreaChart areaChart = new(chartData)
        {
            AltText = "Area chart showing monthly sales performance from January to December",
            Height = 400,
            StretchToFit = true,
            AreaColor = Color.FromHex("#a855f7"),
            FillOpacity = 0.3f,
            LineWidth = 2.5f,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(areaChart);
    }

    private static void AddLollipopChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Lollipop Chart: Quarterly Metrics",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Stems with circular heads - clear and minimal bar alternative",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with quarterly data
        ChartData chartData = new()
        {
            Title = "Quarterly Revenue",
            Series = [new ChartSeries { Color = Color.FromHex("#ec4899"), DataPoints = QuarterlyRevenue }],
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
        LollipopChart lollipopChart = new(chartData)
        {
            AltText = "Lollipop chart showing quarterly revenue metrics",
            Height = 400,
            StretchToFit = true,
            LollipopColor = Color.FromHex("#ec4899"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(lollipopChart);
    }

    private static void AddDotPlotChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Dot Plot Chart: Product Performance",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Circular markers positioned along a value axis",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with product data
        ChartData chartData = new()
        {
            Title = "Product Sales",
            Series = [new ChartSeries { Color = Color.FromHex("#14b8a6"), DataPoints = ProductSales }],
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
        DotPlotChart dotPlotChart = new(chartData)
        {
            AltText = "Dot plot chart showing product sales performance across eight products",
            Height = 400,
            StretchToFit = true,
            DotColor = Color.FromHex("#14b8a6"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#fafafa")
        };

        // Add chart to document
        document.Add(dotPlotChart);
    }

    private static void AddPieChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = $"Pie Chart: Monthly Sales {DateTime.Now.Year}",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Donut chart with legend and total value in the centre",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = $"Monthly Sales {DateTime.Now.Year}",
            Series = [new ChartSeries { DataPoints = PieChartSales }]
        };

        // Create pie chart
        PieChart pieChart = new(chartData)
        {
            AltText = "Pie chart showing proportional distribution of monthly sales",
            Height = 400,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total",
            ShowLegend = true,
            Colors = new[] { "#4f46e5", "#7c3aed", "#a855f7", "#d946ef", "#ec4899", "#f43f5e", "#f97316", "#eab308", "#84cc16", "#22c55e", "#14b8a6", "#06b6d4" },
            BackgroundColor = Color.White
        };

        // Add chart to document
        document.Add(pieChart);
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

    private static IList<ChartDataPoint> QuarterlyRevenue => new List<ChartDataPoint>()
    {
        new ChartDataPoint
        {
            Label = "Q1",
            Value = 4500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Q2",
            Value = 6200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Q3",
            Value = 8500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Q4",
            Value = 11500,
            LabelFormat = "N0"
        }
    };

    private static IList<ChartDataPoint> ProductSales => new List<ChartDataPoint>()
    {
        new ChartDataPoint
        {
            Label = "Prod A",
            Value = 4500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod B",
            Value = 6200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod C",
            Value = 5800,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod D",
            Value = 7200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod E",
            Value = 8500,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod F",
            Value = 9100,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod G",
            Value = 8200,
            LabelFormat = "N0"
        },
        new ChartDataPoint
        {
            Label = "Prod H",
            Value = 9500,
            LabelFormat = "N0"
        }
    };

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
