# Charting

BonniePDF provides a comprehensive charting API for creating data visualizations in PDF documents. The charting system is built on a flexible `ICanvas` interface, making it backend-agnostic and supporting both PDF and SVG rendering.

## Your First Chart

```csharp
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

Pdf.Create(doc =>
{
    doc.WithMetadata(metadata =>
    {
        metadata.Title = "Sales Report";
        metadata.Author = "BonniePDF";
    });

    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            column.Item()
                .Text("Quarterly Sales Report")
                .Bold()
                .FontSize(24)
                .Margin(0, 0, 0, 12);

            var chartData = new ChartData
            {
                Title = "Revenue by Quarter",
                Series = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Revenue",
                        Color = Color.FromHex("#4f46e5"),
                        DataPoints = new List<ChartDataPoint>
                        {
                            new() { Label = "Q1", Value = 15000 },
                            new() { Label = "Q2", Value = 22000 },
                            new() { Label = "Q3", Value = 18000 },
                            new() { Label = "Q4", Value = 25000 }
                        }
                    }
                },
                YAxis = new AxisOptions
                {
                    MaxValue = 30000,
                    Legend = "Revenue ($)",
                    ShowGridLines = true
                }
            };

            var barChart = new BarChart
            {
                Width = 600,
                Height = 400,
                BackgroundColor = Color.White
            };

            column.Item().Element(new ChartElement(barChart, chartData));
        });
    });
}).Save("sales-report.pdf");
```

## Chart Types

### BarChart

Vertical or horizontal bars for comparing values across categories.

```csharp
var chart = new BarChart
{
    Width = 600,
    Height = 400,
    BackgroundColor = Color.White,
    BarWidthRatio = 0.6f,
    ShowValueLabels = true
};

var data = new ChartData
{
    Title = "Quarterly Sales",
    Series = new List<ChartSeries>
    {
        new ChartSeries
        {
            Name = "Revenue",
            Color = Color.FromHex("#4f46e5"),
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Q1", Value = 15000 },
                new() { Label = "Q2", Value = 22000 },
                new() { Label = "Q3", Value = 18000 },
                new() { Label = "Q4", Value = 25000 }
            }
        }
    },
    YAxis = new AxisOptions
    {
        MaxValue = 30000,
        Legend = "Revenue ($)",
        ShowGridLines = true
    }
};
```

### LineChart

Connected data points with optional markers, ideal for showing trends over time.

```csharp
var chart = new LineChart
{
    Width = 600,
    Height = 400,
    DefaultLineWidth = 2f,
    DefaultShowMarkers = true,
    DefaultMarkerRadius = 5f
};

var data = new ChartData
{
    Title = "Website Traffic",
    Series = new List<ChartSeries>
    {
        new ChartSeries
        {
            Name = "Visitors",
            Color = Color.FromHex("#22c55e"),
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Jan", Value = 12000 },
                new() { Label = "Feb", Value = 15000 },
                new() { Label = "Mar", Value = 18000 },
                new() { Label = "Apr", Value = 16000 }
            }
        }
    }
};
```

### AreaChart

Filled areas under lines with transparency, useful for showing volume or magnitude.

```csharp
var chart = new AreaChart
{
    Width = 600,
    Height = 400,
    DefaultFillOpacity = 0.3f,
    DefaultLineWidth = 2f
};

var data = new ChartData
{
    Title = "Server Load",
    Series = new List<ChartSeries>
    {
        new ChartSeries
        {
            Name = "CPU Usage",
            Color = Color.FromHex("#f97316"),
            Style = new ChartSeriesStyle
            {
                FillOpacity = 0.4f,
                LineWidth = 2f
            },
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "00:00", Value = 25 },
                new() { Label = "04:00", Value = 35 },
                new() { Label = "08:00", Value = 65 },
                new() { Label = "12:00", Value = 80 }
            }
        }
    }
};
```

### PieChart

Circular chart with slices representing proportions, supporting donut style with a center hole.

```csharp
var chart = new PieChart
{
    Width = 600,
    Height = 400,
    IsDonut = true,
    TotalLabel = "Total",
    TotalValueFormat = "C0",
    ShowLegend = true,
    Colors = new List<string>
    {
        "#4f46e5", "#7c3aed", "#a855f7", "#d946ef", "#ec4899"
    }
};

var data = new ChartData
{
    Title = "Budget Allocation",
    Series = new List<ChartSeries>
    {
        new ChartSeries
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Marketing", Value = 25000 },
                new() { Label = "Operations", Value = 30000 },
                new() { Label = "R&D", Value = 20000 },
                new() { Label = "Sales", Value = 15000 }
            }
        }
    }
};
```

### GroupedBarChart

Multiple bars per category for comparing multiple datasets.

```csharp
var chart = new GroupedBarChart
{
    Width = 600,
    Height = 400,
    BarWidthRatio = 0.6f,
    GroupGapRatio = 0.2f
};

var data = new ChartData
{
    Title = "Regional Sales Comparison",
    Series = new List<ChartSeries>
    {
        new ChartSeries
        {
            Name = "2023",
            Color = Color.FromHex("#4f46e5"),
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "North", Value = 45000 },
                new() { Label = "South", Value = 38000 },
                new() { Label = "East", Value = 52000 }
            }
        },
        new ChartSeries
        {
            Name = "2024",
            Color = Color.FromHex("#22c55e"),
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "North", Value = 51000 },
                new() { Label = "South", Value = 42000 },
                new() { Label = "East", Value = 58000 }
            }
        }
    }
};
```

## Chart Orientation

All Cartesian charts support vertical and horizontal orientations:

```csharp
// Vertical mode (default)
var verticalChart = new BarChart
{
    Orientation = ChartOrientation.Vertical
};

// Horizontal mode
var horizontalChart = new BarChart
{
    Orientation = ChartOrientation.Horizontal
};
```

Horizontal orientation is useful for long category labels or when data reads more naturally left-to-right.

## Axis Configuration

Configure axis display, scale, and labels using `AxisOptions`:

```csharp
YAxis = new AxisOptions
{
    Prefix = "$",
    MaxValue = 100000,
    StepCount = 5,
    Legend = "Revenue",
    ShowGridLines = true
}
```

For percentage values:

```csharp
YAxis = new AxisOptions
{
    MaxValue = 100,
    Suffix = "%",
    StepCount = 4,
    Legend = "Completion Rate"
}
```

## Multi-Series Charts

Most chart types support multiple series for comparing datasets:

```csharp
var data = new ChartData
{
    Title = "Revenue Comparison",
    Series = new List<ChartSeries>
    {
        new ChartSeries
        {
            Name = "Product A",
            Color = Color.FromHex("#4f46e5"),
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Q1", Value = 15000 },
                new() { Label = "Q2", Value = 22000 }
            }
        },
        new ChartSeries
        {
            Name = "Product B",
            Color = Color.FromHex("#22c55e"),
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Q1", Value = 18000 },
                new() { Label = "Q2", Value = 25000 }
            }
        }
    }
};
```

When multiple series are present, charts automatically display a legend.

## Responsive Layouts

Use `StretchToFit` to make charts adapt to their container width:

```csharp
doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // Chart that stretches to full page width
        column.Item()
            .Padding(20)
            .Background(Color.White)
            .Border(1, Color.Gray300)
            .Element(new ChartElement(
                new BarChart
                {
                    StretchToFit = true,
                    Height = 300,
                    BackgroundColor = null
                },
                chartData
            ));
    });
});
```

## Multiple Charts on a Page

Use horizontal layouts to place charts side by side:

```csharp
doc.Page(page =>
{
    page.Content().Column(column =>
    {
        column.Item()
            .Text("Sales Dashboard")
            .Bold()
            .FontSize(24)
            .Margin(0, 0, 0, 12);

        column.Item().Row(row =>
        {
            row.Spacing(20);

            // Left chart
            row.RelativeItem().Column(col =>
            {
                col.Item()
                    .Text("Revenue Trend")
                    .Bold()
                    .FontSize(14)
                    .Margin(0, 0, 0, 8);

                col.Item().Element(new ChartElement(revenueChart, revenueData));
            });

            // Right chart
            row.RelativeItem().Column(col =>
            {
                col.Item()
                    .Text("Product Breakdown")
                    .Bold()
                    .FontSize(14)
                    .Margin(0, 0, 0, 8);

                col.Item().Element(new ChartElement(pieChart, productData));
            });
        });
    });
});
```

## Required Namespaces

| Namespace | Purpose |
|---|---|
| `BonniePDF.Charts` | Chart types (`BarChart`, `LineChart`, etc.) |
| `BonniePDF.Charts.Models` | Data models (`ChartData`, `ChartSeries`, `AxisOptions`) |
| `BonniePDF.Fluent` | `Pdf.Create()` entry point |
| `BonniePDF.Fluent.Extensions` | Container extensions (`.Column()`, `.Row()`, `.Element()`) |
| `BonniePDF.Extensions` | TextBlock extensions (`.Bold()`, `.FontSize()`, etc.) |
| `BonniePDF.Typography` | `StandardFonts` |
| `BonniePDF.Core` | `Color` |
| `BonniePDF.Configuration` | `PdfVersion`, `PdfAEnabled` |

## Next Steps

- [Getting Started](01-getting-started.md) — introduction to the Fluent API
- [Document Setup](02-document-setup.md) — metadata, settings, and page configuration
- [Page Layout](03-page-layout.md) — columns, rows, and nested structures
- [Text & Typography](04-text-and-typography.md) — fonts, weights, and styles
- [Tables](05-tables.md) — data tables with headers and borders
- [Styling](06-styling.md) — margins, padding, borders, and backgrounds
- [Images & Shapes](07-images-and-shapes.md) — embedding images and shapes
- [Headers & Footers](08-headers-and-footers.md) — page templates with page numbers
- [Advanced Features](09-advanced-features.md) — watermarks, encryption, and bookmarks
- [API Reference](10-api-reference.md) — complete type and enum reference
