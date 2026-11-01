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
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Rounded gradient bars, clear value labels and a modern chart theme",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
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
            Theme = "modern",
            BarStyle = new ChartBarStyle { CornerRadius = 6, Gradient = new ChartLinearGradient { StartColor = Color.FromHex("#60a5fa"), EndColor = Color.FromHex("#2563eb"), Scope = "bar" } },
            AltText = "Bar chart showing monthly sales from January to December with values in pounds sterling",
            Height = 400,
            StretchToFit = true,
            BarColor = Color.FromHex("#2563eb"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            BackgroundColor = Color.FromHex("#ffffff")
        };

        // Add chart to document
        document.Add(barChart);
    }

    private static void AddGroupedBarChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = "Grouped Bar Chart: Monthly Revenue",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Comparison of three product lines across all months",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
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

        ChartSeries electronicsSeries = new() { Name = "Electronics", Color = Color.FromHex("#2563eb") };
        ChartSeries clothingSeries = new() { Name = "Clothing", Color = Color.FromHex("#0d9488") };
        ChartSeries homeSeries = new() { Name = "Home & Garden", Color = Color.FromHex("#7c3aed") };

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
            Theme = "modern",
            BarStyle = new ChartBarStyle { CornerRadius = 3 },
            AltText =
                "Grouped bar chart comparing monthly revenue across Electronics, Clothing, and Home & Garden product lines",
            Height = 400,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#ffffff"),
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
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Connected data points with markers and value labels",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
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
            Theme = "modern",
            AltText = "Line chart showing monthly revenue trends from January to December",
            Height = 400,
            StretchToFit = true,
            LineColor = Color.FromHex("#7c3aed"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#ffffff")
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
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = $"Comparing {DateTime.Now.Year - 1} and {DateTime.Now.Year} revenue with multiple series",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
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
                    Color = Color.FromHex("#2563eb"),
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
            Theme = "modern",
            AltText = "Multi-series line chart comparing year-over-year monthly revenue",
            Height = 400,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#ffffff")
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
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Filled area under the line with configurable opacity",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = "Monthly Sales",
            Series = [new ChartSeries { Color = Color.FromHex("#0d9488"), DataPoints = MonthlySales }],
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
            Theme = "modern",
            AltText = "Area chart showing monthly sales performance from January to December",
            Height = 400,
            StretchToFit = true,
            AreaColor = Color.FromHex("#0d9488"),
            FillOpacity = 0.3f,
            LineWidth = 2.5f,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#ffffff")
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
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Stems with circular heads - clear and minimal bar alternative",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with quarterly data
        ChartData chartData = new()
        {
            Title = "Quarterly Revenue",
            Series = [new ChartSeries { Color = Color.FromHex("#7c3aed"), DataPoints = QuarterlyRevenue }],
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
            Theme = "modern",
            AltText = "Lollipop chart showing quarterly revenue metrics",
            Height = 400,
            StretchToFit = true,
            LollipopColor = Color.FromHex("#7c3aed"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#ffffff")
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
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Circular markers positioned along a value axis",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with product data
        ChartData chartData = new()
        {
            Title = "Product Sales",
            Series = [new ChartSeries { Color = Color.FromHex("#0d9488"), DataPoints = ProductSales }],
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
            Theme = "modern",
            AltText = "Dot plot chart showing product sales performance across eight products",
            Height = 400,
            StretchToFit = true,
            DotColor = Color.FromHex("#0d9488"),
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
            BackgroundColor = Color.FromHex("#ffffff")
        };

        // Add chart to document
        document.Add(dotPlotChart);
    }

    private static void AddPieChart(Document document)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = $"Doughnut Chart: Revenue Mix {DateTime.Now.Year}",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });

        document.Add(new TextBlock
        {
            Text = "Four product lines, a coordinated palette and a clear revenue breakdown",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create chart data with all 12 months
        ChartData chartData = new()
        {
            Title = $"Product Revenue {DateTime.Now.Year}",
            Series = [new ChartSeries { DataPoints = new[] { ("Enterprise", 910000m), ("Professional", 650000m), ("Starter", 446000m), ("Services", 222000m) }.Select(p => new ChartDataPoint { Label = p.Item1, Value = p.Item2, LabelFormat = "N0" }).ToList() }]
        };

        // Create pie chart
        PieChart pieChart = new(chartData)
        {
            Theme = "modern",
            AltText = "Pie chart showing proportional distribution of monthly sales",
            Height = 400,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total revenue (GBP)",
            TotalValueFormat = "N0",
            ShowLegend = true,
            Colors = new[] { "#2563eb", "#0d9488", "#7c3aed", "#d97706" },
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

}
