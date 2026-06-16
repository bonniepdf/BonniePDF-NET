using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using System.Globalization;
using Xunit;

namespace BonniePDF.Tests;

public class SvgExportTests
{
    [Fact]
    public void SvgCanvas_DrawText_UsesTopCoordinateForY()
    {
        var canvas = new SvgCanvas(100, 50);

        canvas.DrawText(10, 20, "Legend", Color.FromHex("#000000"), 10, "Arial, sans-serif", TextAlignment.Left);

        Assert.Contains("y=\"28\"", canvas.GetSvg());
    }

    [Fact]
    public void SvgCanvas_FillEllipse_EmitsEllipseElement()
    {
        var canvas = new SvgCanvas(100, 50);

        canvas.FillEllipse(25, 20, 15, 10, Color.FromHex("#336699"));

        string svg = canvas.GetSvg();
        Assert.Contains("<ellipse", svg);
        Assert.Contains("cx=\"25\"", svg);
        Assert.Contains("cy=\"20\"", svg);
        Assert.Contains("rx=\"15\"", svg);
        Assert.Contains("ry=\"10\"", svg);
        Assert.Contains("fill=\"#336699\"", svg);
    }

    [Fact]
    public void BarChart_ToSvg_ProducesValidSvg()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            Title = "Test Chart",
            Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }, new() { Label = "B", Value = 20 }] }]
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
        Assert.Contains("xmlns", svg);
    }

    private static ChartData SampleChartData() => new()
    {
        Title = "Test Chart",
        Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }, new() { Label = "B", Value = 20 }] }]
    };

    [Fact]
    public void BarChart_ToSvg_UsesCrispEdges()
    {
        // Bars + gridlines are axis-aligned: crisp edges render sharper than antialiased.
        string svg = new BarChart().ToSvg(SampleChartData());
        Assert.Contains("shape-rendering=\"crispEdges\"", svg);
    }

    [Fact]
    public void GroupedBarChart_ToSvg_UsesCrispEdges()
    {
        string svg = new GroupedBarChart().ToSvg(SampleChartData());
        Assert.Contains("shape-rendering=\"crispEdges\"", svg);
    }

    [Fact]
    public void LineChart_ToSvg_KeepsDefaultAntialiasing()
    {
        // Diagonal segments + circle markers must stay antialiased.
        string svg = new LineChart().ToSvg(SampleChartData());
        Assert.DoesNotContain("shape-rendering", svg);
    }

    [Fact]
    public void PieChart_ToSvg_KeepsDefaultAntialiasing()
    {
        // Arc paths must stay antialiased.
        string svg = new PieChart().ToSvg(SampleChartData());
        Assert.DoesNotContain("shape-rendering", svg);
    }

    [Fact]
    public void BarChart_ToSvg_ContainsChartTitle()
    {
        var chart = new BarChart();
        var data = new ChartData
        {
            Title = "My Test Chart",
            Series = [new ChartSeries { DataPoints = [new() { Label = "X", Value = 100 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "Alpha", Value = 50 }, new() { Label = "Beta", Value = 75 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "P1", Value = 10 }, new() { Label = "P2", Value = 20 }, new() { Label = "P3", Value = 15 }] }]
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
        // Line chart uses <line> elements (via DrawLine)
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void AreaChart_ToSvg_RendersAreaFillAsSingleTransparentPath()
    {
        var chart = new AreaChart();
        var data = new ChartData
        {
            Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }, new() { Label = "B", Value = 30 }] }]
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("<path", svg);
        Assert.Contains("fill-opacity=\"0.3\"", svg);
        Assert.DoesNotContain("<rect", svg);
    }

    [Fact]
    public void PieChart_ToSvg_ProducesValidSvg()
    {
        var chart = new PieChart();
        var data = new ChartData
        {
            Title = "Pie Chart",
            Series = [new ChartSeries { DataPoints = [new() { Label = "Slice 1", Value = 30 }, new() { Label = "Slice 2", Value = 70 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "D1", Value = 15 }, new() { Label = "D2", Value = 25 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "L1", Value = 40 }] }]
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        // Lollipop chart should use lines and circles
        Assert.Contains("<line", svg);
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void GroupedBarChart_ToSvg_ProducesValidSvg()
    {
        var chart = new GroupedBarChart();
        var data = new ChartData
        {
            YAxis = { MaxValue = 30 },
            Series =
            [
                new ChartSeries { Name = "A", DataPoints = [new() { Label = "Q1", Value = 10 }, new() { Label = "Q2", Value = 15 }] },
                new ChartSeries { Name = "B", DataPoints = [new() { Label = "Q1", Value = 20 }, new() { Label = "Q2", Value = 25 }] }
            ]
        };

        string svg = chart.ToSvg(data);

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void PieChart_ToSvg_CentersLegendLabelsWithColorBoxes()
    {
        var chart = new PieChart
        {
            Width = 540,
            Height = 320
        };
        var data = new ChartData
        {
            Series =
            [
                new ChartSeries
                {
                    DataPoints = [new() { Label = ".NET", Value = 48 }]
                }
            ]
        };

        string svg = chart.ToSvg(data);
        float rectY = ExtractYAttribute(svg, """width="14" height="14" fill="#4F46E5"/>""");
        float textY = ExtractYAttribute(svg, ".NET - 48</text>");

        Assert.True(Math.Abs(textY - rectY - 10.3f) < 0.001f);
    }

    [Fact]
    public void Chart_ToSvg_WithEmptyData_ProducesValidSvg()
    {
        var chart = new BarChart();
        var data = new ChartData { Title = "Empty Chart" };

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
            Series = [new ChartSeries { DataPoints = [new() { Label = "Test", Value = 1 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "X", Value = 100 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "Red Bar", Value = 50 }] }]
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
            Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 10 }] }],
            XAxis = new AxisOptions { Legend = "X Axis Label" },
            YAxis = new AxisOptions { Legend = "Y Axis Label" }
        };

        string svg = chart.ToSvg(data);

        Assert.Contains("X Axis Label", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Y Axis Label", svg, StringComparison.OrdinalIgnoreCase);
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

    private static float ExtractYAttribute(string svg, string marker)
    {
        int markerIndex = svg.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0);

        int tagStart = svg.LastIndexOf('<', markerIndex);
        Assert.True(tagStart >= 0);

        const string attributeStart = " y=\"";
        int yStart = svg.IndexOf(attributeStart, tagStart, StringComparison.Ordinal);
        Assert.True(yStart >= 0 && yStart < markerIndex);

        yStart += attributeStart.Length;
        int yEnd = svg.IndexOf('"', yStart);
        Assert.True(yEnd > yStart);

        return float.Parse(svg[yStart..yEnd], CultureInfo.InvariantCulture);
    }
}
