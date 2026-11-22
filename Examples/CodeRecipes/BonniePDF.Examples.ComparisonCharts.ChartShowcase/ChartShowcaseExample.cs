using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

/// <summary>
/// A comprehensive example that generates a multipage PDF containing all chart types.
/// </summary>
[System.ComponentModel.DisplayName("Charting Examples")]
[System.ComponentModel.Category("Comparison Charts")]
[System.ComponentModel.Description(
    "A comprehensive 15-page landscape PDF showcasing all 14 chart families including bar, grouped bar, stacked bar, line, area, stacked area, combo, waterfall, scatter, dot plot, lollipop, pie/donut, bullet, and heatmap charts.")]
public static class ChartShowcaseExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(now: null);

    public static Document Generate(DateTimeOffset? now, TimeZoneInfo? timeZone = null)
    {
        DateTime day = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.UtcNow, timeZone ?? TimeZoneInfo.Local).Date;
        int year = day.Year - 1;
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
                outerStack.Add(PageNumber.Text().Font(StandardFonts.Helvetica, 10).Color(ColorPalette.Slate[500]).AlignRight(), 1f); // Takes all remaining space and right-aligns within it

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
        AddPieChart(document, year);

        // 8. Multi-Series Line Chart (on eighth page)
        document.AddPage();
        AddMultiSeriesLineChart(document, year);

        // 9. Stacked Bar Chart
        document.AddPage();
        AddStackedBarChart(document);

        // 10. Stacked Area Chart
        document.AddPage();
        AddStackedAreaChart(document);

        // 11. Combo Chart
        document.AddPage();
        AddComboChart(document);

        // 12. Waterfall Chart
        document.AddPage();
        AddWaterfallChart(document);

        // 13. Scatter / Bubble Chart
        document.AddPage();
        AddScatterChart(document);

        // 14. Bullet Charts & Sparklines
        document.AddPage();
        AddBulletCharts(document);

        // 15. Heatmap Chart
        document.AddPage();
        AddHeatmapChart(document);

        // 16. Dark Theme
        document.AddPage();
        AddDarkThemeChart(document);

        // 17. Print & Monochrome Pattern Theme
        document.AddPage();
        AddPrintThemeChart(document);

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
            AutoLayout = true,
            BarStyle = new ChartBarStyle { CornerRadius = 6, Gradient = new ChartLinearGradient { StartColor = Color.FromHex("#60a5fa"), EndColor = Color.FromHex("#2563eb"), Scope = "bar" } },
            References =
            [
                new ChartReference
                {
                    Value = 9000,
                    Label = "Target £9k",
                    LabelPosition = "end",
                    LabelBackgroundColor = Color.FromHex("#fef3c7"),
                    Color = Color.FromHex("#d97706"),
                    Thickness = 1.5f,
                    DashPattern = [5f, 3f],
                },
            ],
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
            Curve = "smooth",
            DefaultMarkerHaloWidth = 1.5f,
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

    private static void AddMultiSeriesLineChart(Document document, int year)
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
            Text = $"Comparing {year - 1} and {year} revenue with multiple series",
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
                    Name = $"{year - 1}",
                    Color = Color.FromHex("#2563eb"),
                    Style = new ChartSeriesStyle
                    {
                        DashPattern = [6f, 3f],
                        MarkerShape = "diamond",
                        MarkerFill = "hollow",
                        MarkerRadius = 4f,
                    },
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
                    Name = $"{year}",
                    Color = Color.FromHex("#7c3aed"),
                    Style = new ChartSeriesStyle
                    {
                        LineWidth = 2.5f,
                        MarkerShape = "circle",
                        MarkerHaloWidth = 1.5f,
                        MarkerRadius = 4.5f,
                    },
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
                ValueFormat = "K0",
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
            AutoLayout = true,
            Curve = "smooth",
            LegendPosition = "top",
            AltText = "Multi-series line chart comparing year-over-year monthly revenue",
            Height = 400,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = false,
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
            Text = "Smooth curve with vertical area fade gradient",
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
            Curve = "smooth",
            AreaGradient = true,
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

    private static void AddPieChart(Document document, int year)
    {
        // Add a title
        document.Add(new TextBlock
        {
            Text = $"Doughnut Chart: Revenue Mix {year}",
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
            Title = $"Product Revenue {year}",
            Series = [new ChartSeries { DataPoints = new[] { ("Enterprise", 910000m), ("Professional", 650000m), ("Starter", 446000m), ("Services", 222000m) }.Select(p => new ChartDataPoint { Label = p.Item1, Value = p.Item2, LabelFormat = "N0" }).ToList() }]
        };

        // Create pie chart
        PieChart pieChart = new(chartData)
        {
            Theme = "modern",
            SliceGradient = true,
            SliceBorderWidth = 1.5f,
            DonutHoleRatio = 0.56f,
            SliceLabelMode = "callout",
            LegendFormat = "labelValuePercent",
            TotalPosition = "center",
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

    private static void AddPageHeader(Document document, string title, string subtitle)
    {
        document.Add(new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Color.FromHex("#0f172a"),
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });
        document.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 20)
        });
    }

    private static void AddStackedBarChart(Document document)
    {
        AddPageHeader(document, "Stacked Bar Chart: Revenue Mix by Channel",
            "Multi-series stacking with compact K totals, segment gaps and auto-contrast inside labels");
        document.Add(new StackedBarChart
        {
            Theme = "modern",
            Mode = "absolute",
            ShowStackTotals = true,
            StackTotalFormat = "K0",
            ValueLabelFormat = "K0",
            ValueLabelPrefix = "£",
            SegmentGap = 1.5f,
            Height = 400,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Stacked bar chart showing quarterly revenue split across Direct, Partner, and Self-serve channels.",
            Data = new ChartData
            {
                Title = "Quarterly Revenue Mix",
                YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K0" },
                Series =
                [
                    new ChartSeries { Name = "Direct", DataPoints = [new() { Label = "Q1", Value = 180000 }, new() { Label = "Q2", Value = 215000 }, new() { Label = "Q3", Value = 248000 }, new() { Label = "Q4", Value = 292000 }] },
                    new ChartSeries { Name = "Partner", DataPoints = [new() { Label = "Q1", Value = 95000 }, new() { Label = "Q2", Value = 112000 }, new() { Label = "Q3", Value = 128000 }, new() { Label = "Q4", Value = 146000 }] },
                    new ChartSeries { Name = "Self-serve", DataPoints = [new() { Label = "Q1", Value = 64000 }, new() { Label = "Q2", Value = 78000 }, new() { Label = "Q3", Value = 94000 }, new() { Label = "Q4", Value = 118000 }] },
                ],
            },
        });
    }

    private static void AddStackedAreaChart(Document document)
    {
        AddPageHeader(document, "Stacked Area Chart: Platform Traffic Share",
            "100% normalized smooth stacked area bands with inline series labels");
        document.Add(new StackedAreaChart
        {
            Theme = "modern",
            Mode = "percent",
            Curve = "smooth",
            LegendPosition = "inline",
            Height = 400,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "100% stacked area chart showing platform traffic share shifting from Web toward Mobile and API.",
            Data = new ChartData
            {
                Title = "Traffic Share (%)",
                YAxis = new AxisOptions { Suffix = "%" },
                Series =
                [
                    new ChartSeries { Name = "Web", DataPoints = [new() { Label = "Q1", Value = 58 }, new() { Label = "Q2", Value = 54 }, new() { Label = "Q3", Value = 50 }, new() { Label = "Q4", Value = 47 }] },
                    new ChartSeries { Name = "Mobile", DataPoints = [new() { Label = "Q1", Value = 30 }, new() { Label = "Q2", Value = 33 }, new() { Label = "Q3", Value = 36 }, new() { Label = "Q4", Value = 38 }] },
                    new ChartSeries { Name = "API", DataPoints = [new() { Label = "Q1", Value = 12 }, new() { Label = "Q2", Value = 13 }, new() { Label = "Q3", Value = 14 }, new() { Label = "Q4", Value = 15 }] },
                ],
            },
        });
    }

    private static void AddComboChart(Document document)
    {
        AddPageHeader(document, "Combo Chart: Revenue vs. Gross Margin",
            "Primary revenue bars paired with a smooth gross-margin percentage line on a secondary Y axis");
        document.Add(new ComboChart
        {
            Theme = "modern",
            ShowValueLabels = false,
            LegendPosition = "top",
            SecondaryYAxis = new AxisOptions { MinValue = 50, MaxValue = 90, Suffix = "%", ValueFormat = "F0" },
            Height = 400,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Combo chart comparing quarterly revenue bars against gross margin percentage on a secondary axis.",
            Data = new ChartData
            {
                Title = "Quarterly Revenue & Gross Margin",
                YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K0" },
                Series =
                [
                    new ChartSeries { Name = "Revenue", SeriesType = "bar", ValueAxis = "primary", DataPoints = [new() { Label = "Q1", Value = 420000 }, new() { Label = "Q2", Value = 485000 }, new() { Label = "Q3", Value = 540000 }, new() { Label = "Q4", Value = 635000 }] },
                    new ChartSeries
                    {
                        Name = "Gross Margin",
                        SeriesType = "line",
                        ValueAxis = "secondary",
                        Color = Color.FromHex("#f59e0b"),
                        Style = new ChartSeriesStyle { Curve = "smooth", LineWidth = 2.5f, MarkerFill = "hollow", MarkerHaloWidth = 1.5f },
                        DataPoints = [new() { Label = "Q1", Value = 68 }, new() { Label = "Q2", Value = 71 }, new() { Label = "Q3", Value = 74 }, new() { Label = "Q4", Value = 79 }],
                    },
                ],
            },
        });
    }

    private static void AddWaterfallChart(Document document)
    {
        AddPageHeader(document, "Waterfall Chart: ARR Movement Bridge",
            "Cumulative variance bridge with positive/negative deltas, auto-computed ending total and connectors");
        document.Add(new WaterfallChart
        {
            Theme = "modern",
            ValueLabelPrefix = "£",
            ValueLabelFormat = "K1",
            Height = 400,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Waterfall bridge from 800k starting ARR through new business, expansion, churn and contraction to 1.07M.",
            Data = new ChartData
            {
                Title = "Annual Recurring Revenue Bridge",
                YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K1" },
                Series =
                [
                    new ChartSeries
                    {
                        Name = "ARR",
                        DataPoints =
                        [
                            new() { Label = "Start", Value = 800000, Kind = "total" },
                            new() { Label = "New", Value = 260000, Kind = "delta" },
                            new() { Label = "Expansion", Value = 135000, Kind = "delta" },
                            new() { Label = "Churn", Value = -85000, Kind = "delta" },
                            new() { Label = "Contraction", Value = -40000, Kind = "delta" },
                            new() { Label = "Ending", Kind = "total" },
                        ],
                    },
                ],
            },
        });
    }

    private static void AddScatterChart(Document document)
    {
        AddPageHeader(document, "Scatter Chart: Latency vs. Throughput",
            "Continuous numeric X/Y coordinates with area-scaled bubbles and OLS linear trendline");
        document.Add(new ScatterChart
        {
            Theme = "modern",
            ShowTrendline = "linear",
            Height = 400,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Scatter bubble chart of latency versus throughput for EU and US regions with linear trendlines.",
            Data = new ChartData
            {
                Title = "Cluster Scaling Profile",
                XAxis = new AxisOptions { Legend = "Requests / sec", AutoScale = true },
                YAxis = new AxisOptions { Legend = "p95 Latency (ms)", AutoScale = true },
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Region EU",
                        DataPoints =
                        [
                            new() { Label = "n1", X = 120, Value = 14, Size = 12 },
                            new() { Label = "n2", X = 240, Value = 21, Size = 22 },
                            new() { Label = "n3", X = 360, Value = 29, Size = 34 },
                            new() { Label = "n4", X = 480, Value = 41, Size = 48 },
                        ],
                    },
                    new ChartSeries
                    {
                        Name = "Region US",
                        DataPoints =
                        [
                            new() { Label = "u1", X = 150, Value = 18, Size = 15 },
                            new() { Label = "u2", X = 290, Value = 27, Size = 28 },
                            new() { Label = "u3", X = 420, Value = 37, Size = 40 },
                            new() { Label = "u4", X = 520, Value = 49, Size = 54 },
                        ],
                    },
                ],
            },
        });
    }

    private static void AddBulletCharts(Document document)
    {
        AddPageHeader(document, "Bullet Charts & Sparklines: Executive KPI Scorecard",
            "Compact KPI gauges paired with 12-month enriched trend sparklines highlighting min/max and target baselines");
        VerticalStack stack = new() { Spacing = 18 };

        HorizontalStack row1 = new() { Spacing = 16, WidthDistribution = WidthDistribution.Weighted };
        row1.Add(new BulletChart
        {
            Theme = "modern",
            Label = "Revenue (£k)",
            Value = 275,
            Target = 250,
            Ranges = [new() { Max = 150 }, new() { Max = 225 }, new() { Max = 300 }],
            ShowScale = true,
            ShowValueLabel = true,
            ValuePrefix = "£",
            ValueSuffix = "k",
            Width = 520,
            Height = 64,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Bullet chart showing revenue at 275k against a 250k target.",
        }, 3f);
        row1.Add(CreateSparkline("#2563eb", 250, [195, 204, 212, 208, 224, 236, 241, 248, 255, 262, 269, 275],
            "Twelve-month revenue sparkline rising from 195k to 275k."), 1f);
        stack.Add(row1);

        HorizontalStack row2 = new() { Spacing = 16, WidthDistribution = WidthDistribution.Weighted };
        row2.Add(new BulletChart
        {
            Theme = "modern",
            Label = "Gross Margin",
            Value = 74,
            Target = 78,
            Ranges = [new() { Max = 50 }, new() { Max = 75 }, new() { Max = 100 }],
            ShowScale = true,
            ShowValueLabel = true,
            ValueSuffix = "%",
            MeasureColor = Color.FromHex("#0d9488"),
            Width = 520,
            Height = 64,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Bullet chart showing gross margin at 74% against a 78% target.",
        }, 3f);
        row2.Add(CreateSparkline("#0d9488", 78, [68, 69, 71, 70, 72, 73, 71, 72, 73, 75, 74, 74],
            "Twelve-month gross margin sparkline trending from 68% to 74%."), 1f);
        stack.Add(row2);

        HorizontalStack row3 = new() { Spacing = 16, WidthDistribution = WidthDistribution.Weighted };
        row3.Add(new BulletChart
        {
            Theme = "modern",
            Label = "Net Promoter",
            Value = 64,
            Target = 60,
            Ranges = [new() { Max = 30 }, new() { Max = 55 }, new() { Max = 80 }],
            ShowScale = true,
            ShowValueLabel = true,
            MeasureColor = Color.FromHex("#7c3aed"),
            Width = 520,
            Height = 64,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Bullet chart showing Net Promoter Score at 64 against a target of 60.",
        }, 3f);
        row3.Add(CreateSparkline("#7c3aed", 60, [51, 53, 52, 55, 57, 58, 59, 61, 60, 62, 63, 64],
            "Twelve-month Net Promoter Score sparkline climbing from 51 to 64."), 1f);
        stack.Add(row3);

        document.Add(stack);
    }

    private static LineChart CreateSparkline(string color, decimal target, decimal[] values, string altText)
    {
        List<string> months = MonthLabels;
        ChartSeries series = new() { Color = Color.FromHex(color) };
        for (int i = 0; i < months.Count; i++)
        {
            series.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = values[i], LabelFormat = "N0" });
        }

        return new LineChart(new ChartData { Series = [series] })
        {
            Theme = "modern",
            Sparkline = true,
            Curve = "smooth",
            LineColor = Color.FromHex(color),
            SparklineFillOpacity = 0.18f,
            SparklineShowEndpoints = true,
            SparklineHighlightMinMax = true,
            SparklineReferenceValue = target,
            Height = 64,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = altText,
        };
    }

    private static void AddHeatmapChart(Document document)
    {
        AddPageHeader(document, "Heatmap Chart: Monthly Cohort Retention",
            "2D matrix colour ramp with missing cells, continuous legend and WCAG auto-contrast values");
        document.Add(new HeatmapChart
        {
            Theme = "modern",
            Title = "Cohort Retention (%)",
            XCategories = ["M0", "M1", "M2", "M3", "M4", "M5"],
            YCategories = ["Jan", "Feb", "Mar", "Apr", "May"],
            Values =
            [
                [100m, 84m, 76m, 71m, 68m, 65m],
                [100m, 86m, 79m, 74m, 70m, null],
                [100m, 88m, 82m, 77m, null, null],
                [100m, 90m, 84m, null, null, null],
                [100m, 91m, null, null, null, null],
            ],
            ValueSuffix = "%",
            Height = 400,
            StretchToFit = true,
            BackgroundColor = Color.White,
            AltText = "Cohort retention heatmap from January to May across months M0 through M5.",
        });
    }

    private static void AddDarkThemeChart(Document document)
    {
        AddPageHeader(document, "Dark Theme: Cloud Telemetry Throughput",
            "Built-in dark slate theme with rounded card corners, smooth spline and glowing area fade gradient");
        document.Add(new AreaChart(new ChartData
        {
            Title = "Ingested Telemetry Volume",
            Series = [new ChartSeries { Color = Color.FromHex("#38bdf8"), DataPoints = MonthlySales }],
            YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K0", MaxValue = 15000, StepCount = 5, Legend = "Spend" },
            XAxis = new AxisOptions { Legend = "Month" },
        })
        {
            Theme = "dark",
            AutoLayout = true,
            Curve = "smooth",
            AreaGradient = true,
            BorderRadius = 10,
            BorderColor = Color.FromHex("#334155"),
            BorderWidth = 1,
            Subtitle = "Monthly ingested events across production clusters",
            AltText = "Dark-themed area chart showing telemetry spend rising from 4,500 to 12,800 GBP.",
            Height = 400,
            StretchToFit = true,
            AreaColor = Color.FromHex("#38bdf8"),
            FillOpacity = 0.32f,
            LineWidth = 2.5f,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            ShowValueLabels = true,
        });
    }

    private static void AddPrintThemeChart(Document document)
    {
        AddPageHeader(document, "Print Theme: Monochrome Hatch Patterns",
            "High-contrast grayscale palette with pure-vector diagonal, crosshatch and dot pattern fills for black-and-white printing");
        string[] quarters = ["Q1", "Q2", "Q3", "Q4"];
        decimal[] divA = [14500, 21800, 22500, 31500];
        decimal[] divB = [10500, 14200, 14100, 19400];
        decimal[] divC = [9300, 13000, 12900, 17400];

        ChartSeries sA = new() { Name = "Division A (Diagonal)" };
        ChartSeries sB = new() { Name = "Division B (Crosshatch)" };
        ChartSeries sC = new() { Name = "Division C (Dots)" };
        for (int i = 0; i < quarters.Length; i++)
        {
            sA.DataPoints.Add(new ChartDataPoint { Label = quarters[i], Value = divA[i] });
            sB.DataPoints.Add(new ChartDataPoint { Label = quarters[i], Value = divB[i] });
            sC.DataPoints.Add(new ChartDataPoint { Label = quarters[i], Value = divC[i] });
        }

        document.Add(new GroupedBarChart(new ChartData
        {
            Title = "Quarterly Output by Division",
            YAxis = new AxisOptions { Prefix = "£", ValueFormat = "K0", MaxValue = 35000, StepCount = 7 },
            Series = [sA, sB, sC],
        })
        {
            Theme = "print",
            AutoLayout = true,
            LegendPosition = "top",
            ShowValueLabels = false,
            BorderRadius = 8,
            BorderColor = Color.FromHex("#cbd5e1"),
            BorderWidth = 1,
            BarStyle = new ChartBarStyle { CornerRadius = 3 },
            AltText = "Monochrome print-themed grouped bar chart with diagonal, crosshatch, and dot pattern fills across four quarters.",
            Height = 400,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            BackgroundColor = Color.White,
        });
    }

}
