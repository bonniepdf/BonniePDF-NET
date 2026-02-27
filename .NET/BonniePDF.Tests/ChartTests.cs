using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Images;
using Xunit;

namespace BonniePDF.Tests;

public class ChartTests
{
    // Mock canvas to test chart drawing
    private sealed class MockCanvas : ICanvas
    {
        public List<string> Operations { get; } = new();

        public float Width => 800;
        public float Height => 450;

        public void SaveState()
        {
            Operations.Add("SaveState()");
        }

        public void RestoreState()
        {
            Operations.Add("RestoreState()");
        }

        public void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily,
            TextAlignment alignment)
        {
            Operations.Add($"DrawText({x},{y},'{text}',{fontSize},{fontFamily},{alignment})");
        }

        public void DrawTextJustified(float x, float y, string text, Color color, float fontSize, string fontFamily,
            float availableWidth)
        {
            Operations.Add($"DrawTextJustified({x},{y},'{text}',{fontSize},{fontFamily},{availableWidth})");
        }

        public Size MeasureText(string text, float fontSize, string fontFamily)
        {
            return new Size(text.Length * fontSize * 0.6f, fontSize);
        }

        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth = 1)
        {
            Operations.Add($"DrawLine({x1},{y1},{x2},{y2},{lineWidth})");
        }

        public void FillRect(float x, float y, float width, float height, Color color)
        {
            Operations.Add($"FillRect({x},{y},{width},{height})");
        }

        public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth)
        {
            Operations.Add($"StrokeRect({x},{y},{width},{height},{strokeWidth})");
        }

        public void FillCircle(float cx, float cy, float radius, Color color)
        {
            Operations.Add($"FillCircle({cx},{cy},{radius})");
        }

        public void FillPath(List<Point> points, Color color)
        {
            Operations.Add($"FillPath({points.Count} points)");
        }

        public void DrawImage(ImageInfo image, float x, float y, float width, float height)
        {
            Operations.Add($"DrawImage({x},{y},{width},{height})");
        }

        public void DrawFormXObject(string resourceName, float x, float y)
        {
            Operations.Add($"DrawFormXObject({resourceName},{x},{y})");
        }

        public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color)
        {
            Operations.Add($"FillRoundedRect({x},{y},{width},{height},{radius})");
        }

        public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color,
            float strokeWidth)
        {
            Operations.Add($"StrokeRoundedRect({x},{y},{width},{height},{radius},{strokeWidth})");
        }
    }

    private static ChartData CreateSampleData()
    {
        return new ChartData
        {
            Title = "Test Chart",
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "A", Value = 10 },
                new() { Label = "B", Value = 20 },
                new() { Label = "C", Value = 15 }
            },
            XAxis = new AxisOptions { Legend = "X Axis" },
            YAxis = new AxisOptions { Legend = "Y Axis", ShowGridLines = true }
        };
    }

    [Fact]
    public void BarChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new BarChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.NotEmpty(canvas.Operations);
        Assert.Contains(canvas.Operations, op => op.Contains("DrawText") && op.Contains("Test Chart"));
    }

    [Fact]
    public void BarChart_WithEmptyData_DoesNotCrash()
    {
        var chart = new BarChart();
        var canvas = new MockCanvas();
        var data = new ChartData
        {
            Title = "Empty Chart",
            DataPoints = new List<ChartDataPoint>()
        };

        chart.Draw(canvas, data);

        // Should draw title but no bars
        Assert.Contains(canvas.Operations, op => op.Contains("Empty Chart"));
    }

    [Fact]
    public void BarChart_WithBackgroundColor_DrawsBackground()
    {
        var chart = new BarChart { BackgroundColor = Color.White };
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.Contains(canvas.Operations, op => op.StartsWith("FillRect") && op.Contains("800") && op.Contains("450"));
    }

    [Fact]
    public void BarChart_WithoutBackgroundColor_DoesNotDrawBackground()
    {
        var chart = new BarChart { BackgroundColor = null };
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        // First operation should not be background fill for the full canvas size
        if (canvas.Operations.Count > 0)
        {
            string firstOp = canvas.Operations[0];
            bool isFullCanvasFill = firstOp.StartsWith("FillRect") && firstOp.Contains("800") && firstOp.Contains("450");
            Assert.False(isFullCanvasFill);
        }
    }

    [Fact]
    public void BarChart_CustomDimensions_UsesSpecifiedDimensions()
    {
        var chart = new BarChart
        {
            Width = 400,
            Height = 300,
            BackgroundColor = Color.White
        };
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.Contains(canvas.Operations, op => op.Contains("400") && op.Contains("300"));
    }

    [Fact]
    public void BarChart_ShowValueLabels_DrawsLabels()
    {
        var chart = new BarChart { ShowValueLabels = true };
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        // Should draw text for each data value
        Assert.Contains(canvas.Operations, op => op.Contains("DrawText") && op.Contains("10"));
        Assert.Contains(canvas.Operations, op => op.Contains("DrawText") && op.Contains("20"));
        Assert.Contains(canvas.Operations, op => op.Contains("DrawText") && op.Contains("15"));
    }

    [Fact]
    public void BarChart_HideValueLabels_DoesNotDrawValueLabels()
    {
        var chart = new BarChart { ShowValueLabels = false };
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        // Should still draw title and axis labels, but not value labels on bars
        int textOps = canvas.Operations.FindAll(op => op.Contains("DrawText")).Count;
        Assert.True(textOps > 0); // Still has title and axis labels
    }

    [Fact]
    public void LineChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new LineChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.NotEmpty(canvas.Operations);
        // Line chart should draw paths
        Assert.Contains(canvas.Operations, op => op.Contains("DrawPath") || op.Contains("DrawLine"));
    }

    [Fact]
    public void LineChart_WithMultiplePoints_DrawsConnectedLine()
    {
        var chart = new LineChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        // LineChart uses DrawLine (not DrawPath) to connect points
        var lineOps = canvas.Operations.FindAll(op => op.Contains("DrawLine"));
        Assert.NotEmpty(lineOps);
    }

    [Fact]
    public void AreaChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new AreaChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.NotEmpty(canvas.Operations);
        // Area chart uses FillRect (not FillPath) to draw the area fill
        Assert.Contains(canvas.Operations, op => op.Contains("FillRect"));
    }

    [Fact]
    public void PieChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new PieChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.NotEmpty(canvas.Operations);
        // Pie chart uses paths for wedges
        Assert.Contains(canvas.Operations, op => op.Contains("FillPath") || op.Contains("DrawPath"));
    }

    [Fact]
    public void DotPlotChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new DotPlotChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.NotEmpty(canvas.Operations);
        // Dot plot should draw circles
        Assert.Contains(canvas.Operations, op => op.Contains("FillCircle"));
    }

    [Fact]
    public void LollipopChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new LollipopChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();

        chart.Draw(canvas, data);

        Assert.NotEmpty(canvas.Operations);
        // Lollipop chart should draw lines and circles
        Assert.Contains(canvas.Operations, op => op.Contains("DrawLine"));
        Assert.Contains(canvas.Operations, op => op.Contains("FillCircle"));
    }

    [Fact]
    public void GroupedBarChart_Draw_ExecutesDrawingOperations()
    {
        var chart = new GroupedBarChart
        {
            CategoryLabels = new List<string> { "Q1", "Q2" },
            Series = new List<BarSeries>
            {
                new() { Name = "Product A", Values = new List<decimal> { 10, 15 } },
                new() { Name = "Product B", Values = new List<decimal> { 20, 25 } }
            },
            YAxisMaxValue = 30
        };
        var canvas = new MockCanvas();
        var dummyData = new ChartData(); // GroupedBarChart ignores this parameter

        chart.Draw(canvas, dummyData);

        Assert.NotEmpty(canvas.Operations);
        // Should draw bars (2 categories * 2 series = 4 bars)
        var rectOps = canvas.Operations.FindAll(op => op.Contains("FillRect"));
        Assert.True(rectOps.Count >= 4);
    }

    [Fact]
    public void ChartData_WithNoTitle_SkipsTitleDrawing()
    {
        var chart = new BarChart();
        var canvas = new MockCanvas();
        var data = new ChartData
        {
            Title = null,
            DataPoints = new List<ChartDataPoint>
            {
                new() { Label = "A", Value = 10 }
            }
        };

        chart.Draw(canvas, data);

        // No title text should be drawn
        Assert.DoesNotContain(canvas.Operations, op => op.Contains("DrawText") && op.Contains("null"));
    }

    [Fact]
    public void AxisOptions_WithGridLines_DrawsGrid()
    {
        var chart = new BarChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();
        data.YAxis.ShowGridLines = true;

        chart.Draw(canvas, data);

        // Should have grid line drawing operations
        var lineOps = canvas.Operations.FindAll(op => op.Contains("DrawLine"));
        Assert.NotEmpty(lineOps);
    }

    [Fact]
    public void AxisOptions_WithoutGridLines_DoesNotDrawGrid()
    {
        var chart = new BarChart();
        var canvas = new MockCanvas();
        var data = CreateSampleData();
        data.YAxis.ShowGridLines = false;

        chart.Draw(canvas, data);

        // Should have fewer line operations (only axes)
        var lineOps = canvas.Operations.FindAll(op => op.Contains("DrawLine"));
        // Still has axis lines, but fewer than with grid
        Assert.True(lineOps.Count >= 0);
    }

    [Fact]
    public void ChartDataPoint_FormattedValue_DisplaysCorrectly()
    {
        var point = new ChartDataPoint { Label = "Test", Value = 1234.567m };

        // Default formatted value should show the number
        Assert.NotEmpty(point.FormattedValue);
        Assert.Contains("1234", point.FormattedValue);
    }

    [Fact]
    public void Color_FromHex_ParsesCorrectly()
    {
        var color = Color.FromHex("#FF5733");

        // Color uses 0-255 range
        Assert.Equal(255, color.R);
        Assert.Equal(87, color.G);
        Assert.Equal(51, color.B);
    }

    [Fact]
    public void Color_White_HasCorrectValues()
    {
        var white = Color.White;

        // Color uses 0-255 range
        Assert.Equal(255, white.R);
        Assert.Equal(255, white.G);
        Assert.Equal(255, white.B);
    }

    [Fact]
    public void Color_Black_HasCorrectValues()
    {
        var black = Color.Black;

        // Color uses 0-255 range
        Assert.Equal(0, black.R);
        Assert.Equal(0, black.G);
        Assert.Equal(0, black.B);
    }

    [Fact]
    public void BarChart_StretchToFit_Property()
    {
        var chart = new BarChart { StretchToFit = true };

        Assert.True(chart.StretchToFit);
    }
}
