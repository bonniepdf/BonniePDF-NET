using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Area chart that renders a filled polygon with a line overlay.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class AreaChart : CartesianChartBase
{
    private const string AreaColorHex = "#4f46e5";

    /// <summary>
    /// Color for the area fill and line
    /// </summary>
    public Color AreaColor { get; set; } = Color.FromHex(AreaColorHex);

    /// <summary>
    /// Opacity for the area fill (0.0 to 1.0)
    /// </summary>
    public float FillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Width of the line stroke
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Font size for value labels above data points
    /// </summary>
    public float LabelFontSize { get; set; } = 9;

    /// <summary>
    /// Whether to show value labels above each data point
    /// </summary>
    public bool ShowValueLabels { get; set; } = true;

    /// <summary>
    /// Draws the area chart content.
    /// </summary>
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int dataPointCount = data.DataPoints.Count;
        if (dataPointCount == 0) return;

        // Calculate dynamic layout based on chart dimensions
        (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) = CalculateLayout();

        // Calculate coordinates for all data points
        List<(float x, float y)> coordinates = CalculateCoordinates(data, chartAreaWidth, chartAreaHeight, baselineY);

        // Draw grid lines first (behind everything)
        if (data.YAxis.ShowGridLines) DrawGridLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // Draw axis lines
        DrawAxisLines(canvas, chartAreaWidth, baselineY, topY);

        // Draw Y-axis labels
        DrawYAxisLabels(canvas, data, chartAreaHeight, baselineY, topY);

        // Draw Y-axis legend
        if (!string.IsNullOrEmpty(data.YAxis.Legend)) DrawYAxisLegend(canvas, data.YAxis.Legend, chartAreaHeight, topY);

        // Draw the area fill (behind the line)
        DrawAreaFill(canvas, coordinates, baselineY);

        // Draw the line on top
        DrawConnectingLine(canvas, coordinates);

        // Draw value labels above data points
        if (ShowValueLabels) DrawValueLabels(canvas, data, coordinates);

        // Draw X-axis labels (data point labels)
        DrawXAxisLabels(canvas, data, coordinates, baselineY);

        // Draw X-axis legend
        if (!string.IsNullOrEmpty(data.XAxis.Legend))
            DrawXAxisLegend(canvas, data.XAxis.Legend, chartAreaWidth, baselineY);
    }

    /// <summary>
    /// Draws the area fill using a series of thin vertical rectangles.
    /// This approximates the area under the line connecting data points.
    /// </summary>
    private void DrawAreaFill(ICanvas canvas, List<(float x, float y)> coordinates, float baselineY)
    {
        if (coordinates.Count < 2) return;

        // Create a semi-transparent color for the fill
        Color fillColor = AreaColor.WithAlpha(FillOpacity);

        // Use a small step size for smooth area fill
        const float stepSize = 2f;

        // Find the x range
        float minX = coordinates[0].x;
        float maxX = coordinates[coordinates.Count - 1].x;

        // Draw vertical rectangles from baseline to the line at each step
        for (float x = minX; x < maxX; x += stepSize)
        {
            // Interpolate to find the y value on the line at this x position
            float y = InterpolateY(coordinates, x);

            // Draw a thin rectangle from baseline to the line
            float rectHeight = baselineY - y;
            canvas.FillRect(x, y, stepSize, rectHeight, fillColor);
        }

        // Fill any remaining gap at the end
        float lastX = maxX - (maxX - minX) % stepSize;
        if (lastX < maxX)
        {
            float y = InterpolateY(coordinates, maxX);
            float rectHeight = baselineY - y;
            canvas.FillRect(lastX, y, maxX - lastX, rectHeight, fillColor);
        }
    }

    /// <summary>
    /// Interpolates the y value for a given x position on the line connecting data points.
    /// </summary>
    private float InterpolateY(List<(float x, float y)> coordinates, float x)
    {
        // Find the two data points that x lies between
        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            (float x1, float y1) = coordinates[i];
            (float x2, float y2) = coordinates[i + 1];

            if (x >= x1 && x <= x2)
            {
                // Linear interpolation
                float t = (x - x1) / (x2 - x1);
                return y1 + t * (y2 - y1);
            }
        }

        // If x is outside the range, return the closest point's y
        if (x < coordinates[0].x)
            return coordinates[0].y;
        return coordinates[coordinates.Count - 1].y;
    }

    /// <summary>
    /// Draws the connecting line on top of the area fill.
    /// </summary>
    private void DrawConnectingLine(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        if (coordinates.Count < 2) return;

        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            (float x1, float y1) = coordinates[i];
            (float x2, float y2) = coordinates[i + 1];

            canvas.DrawLine(x1, y1, x2, y2, AreaColor, LineWidth);
        }
    }

    /// <summary>
    /// Draws value labels above each data point.
    /// </summary>
    private void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
        Color labelColor = AreaColor;

        for (int i = 0; i < data.DataPoints.Count; i++)
        {
            (float x, float y) = coordinates[i];
            string text = data.DataPoints[i].FormattedValue;

            // Draw text above the data point
            canvas.DrawText(x, y - 7 - LabelFontSize, text, labelColor, LabelFontSize, FontFamily,
                TextAlignment.Center);
        }
    }
}