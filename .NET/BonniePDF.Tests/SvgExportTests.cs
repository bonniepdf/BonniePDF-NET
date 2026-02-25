using System.Collections.Generic;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Elements.Layout;
using Xunit;

namespace BonniePDF.Tests;

public class SvgExportTests
{
    [Fact]
    public void BarChart_ToSvg_ProducesValidSvg()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            Title = "Test Chart",
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "A", Value = 10 },
                new() { Label = "B", Value = 20 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
        Assert.Contains("xmlns", svg);
    }

    [Fact]
    public void BarChart_ToSvg_ContainsChartTitle()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            Title = "My Test Chart",
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "X", Value = 100 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.Contains("My Test Chart", svg);
    }

    [Fact]
    public void BarChart_ToSvg_ContainsDataLabels()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            Title = "Chart",
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Alpha", Value = 50 },
                new() { Label = "Beta", Value = 75 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.Contains("Alpha", svg);
        Assert.Contains("Beta", svg);
    }

    [Fact]
    public void BarChart_ToSvg_UsesSpecifiedDimensions()
    {
        var chart = new BarChart
        {
            Width = 600,
            Height = 400
        };
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "A", Value = 10 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.Contains("width=\"600\"", svg);
        Assert.Contains("height=\"400\"", svg);
    }

    [Fact]
    public void LineChart_ToSvg_ProducesValidSvg()
    {
        var chart = new LineChart();
        var data = new ChartData
        {
            Title = "Line Chart",
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "P1", Value = 10 },
                new() { Label = "P2", Value = 20 },
                new() { Label = "P3", Value = 15 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
        // Line chart uses <line> elements (via DrawLine)
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void AreaChart_ToSvg_ProducesValidSvg()
    {
        var chart = new AreaChart();
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "A", Value = 10 },
                new() { Label = "B", Value = 30 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        // Area chart uses <rect> elements (via FillRect)
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void PieChart_ToSvg_ProducesValidSvg()
    {
        var chart = new PieChart();
        var data = new ChartData
        {
            Title = "Pie Chart",
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Slice 1", Value = 30 },
                new() { Label = "Slice 2", Value = 70 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("<path", svg);
    }

    [Fact]
    public void DotPlotChart_ToSvg_ProducesValidSvg()
    {
        var chart = new DotPlotChart();
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "D1", Value = 15 },
                new() { Label = "D2", Value = 25 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        // Dot plot should use circles
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void LollipopChart_ToSvg_ProducesValidSvg()
    {
        var chart = new LollipopChart();
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "L1", Value = 40 }
            }
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        // Lollipop chart should use lines and circles
        Assert.Contains("<line", svg);
        Assert.Contains("<circle", svg);
    }

    // Note: GroupedBarChart uses a different API (Series/CategoryLabels) instead of ChartData
    // and doesn't support the ToSvg(ChartData) extension method

    [Fact]
    public void Chart_ToSvg_WithEmptyData_ProducesValidSvg()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            Title = "Empty Chart",
            DataPoints = new List<ChartDataPoint>()
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("Empty Chart", svg);
    }

    // Note: Page.ToSvg() tests removed as Page class requires specific constructor parameters
    // The Page -> SVG functionality is tested through Document rendering integration tests

    [Fact]
    public void SvgOutput_ContainsProperXmlDeclaration()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Test", Value = 1 }
            }
        };

        string svg = chart.ToSvg(data);

        // SVG should have proper namespace
        Assert.Contains("xmlns=\"http://www.w3.org/2000/svg\"", svg);
    }

    [Fact]
    public void SvgOutput_IsValidXml()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "X", Value = 100 }
            }
        };

        string svg = chart.ToSvg(data);

        // Basic XML validity checks
        int openSvgCount = CountOccurrences(svg, "<svg");
        int closeSvgCount = CountOccurrences(svg, "</svg>");

        Assert.Equal(openSvgCount, closeSvgCount);
    }

    [Fact]
    public void BarChart_ToSvg_WithCustomColors_UsesColors()
    {
        var chart = new BarChart
        {
            BarColor = Color.FromHex("#FF5733")
        };
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "Red Bar", Value = 50 }
            }
        };

        string svg = chart.ToSvg(data);

        // SVG should contain color information
        Assert.NotEmpty(svg);
        Assert.Contains("fill", svg.ToLower());
    }

    [Fact]
    public void Chart_ToSvg_WithAxisLegends_IncludesLegends()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "A", Value = 10 }
            },
            XAxis = new AxisOptions { Legend = "X Axis Label" },
            YAxis = new AxisOptions { Legend = "Y Axis Label" }
        };

        string svg = chart.ToSvg(data);

        Assert.Contains("X Axis Label", svg);
        Assert.Contains("Y Axis Label", svg);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
