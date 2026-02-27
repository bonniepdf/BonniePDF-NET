using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;

namespace BonniePDF.Charts;

/// <summary>
/// Area chart that renders a filled polygon with a line overlay.
/// Now supports multiple series with transparency for overlapping areas.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class AreaChart : CartesianChartBase
{
    private const string DefaultAreaColorHex = "#4f46e5";

    /// <summary>
    /// Default fill opacity for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultFillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Default line width for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultLineWidth { get; set; } = 2f;

    /// <summary>
    /// Color for the area fill and line (backward compatibility for single-series charts).
    /// </summary>
    public Color AreaColor { get; set; } = Color.FromHex(DefaultAreaColorHex);

    /// <summary>
    /// Opacity for the area fill (backward compatibility for single-series charts).
    /// </summary>
    public float FillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Width of the line stroke (backward compatibility for single-series charts).
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Draws the area chart elements for a single series (area fill and connecting line).
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Get series-specific styling or use defaults
        float fillOpacity = series.Style?.FillOpacity > 0 ? series.Style.FillOpacity : DefaultFillOpacity;
        float lineWidth = series.Style?.LineWidth > 0 ? series.Style.LineWidth : DefaultLineWidth;

        // Draw the area fill (behind the line)
        DrawAreaFill(canvas, coordinates, baselineY, series.Color, fillOpacity);

        // Draw the line on top
        DrawConnectingLine(canvas, coordinates, series.Color, lineWidth);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (simple spacing above area/line).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws the area fill using a series of thin vertical rectangles.
    /// This approximates the area under the line connecting data points.
    /// </summary>
    private static void DrawAreaFill(ICanvas canvas, IList<Point> coordinates, float baselineY, Color color, float opacity)
    {
        if (coordinates.Count < 2) return;

        // Create a semi-transparent color for the fill
        Color fillColor = color.WithAlpha(opacity);

        // Use a small step size for smooth area fill
        const float stepSize = 2f;

        // Find the x range
        float minX = coordinates[0].X;
        float maxX = coordinates[coordinates.Count - 1].Y;

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
    private static float InterpolateY(IList<Point> coordinates, float x)
    {
        // Find the two data points that x lies between
        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            var p1 = coordinates[i];
            var p2 = coordinates[i + 1];

            if (x >= p1.X && x <= p2.X)
            {
                // Linear interpolation
                float t = (x - p1.X) / (p2.X - p1.X);
                return p1.Y + t * (p2.Y - p1.Y);
            }
        }

        // If x is outside the range, return the closest point's y
        if (x < coordinates[0].X)
            return coordinates[0].Y;
        return coordinates[coordinates.Count - 1].Y;
    }

    /// <summary>
    /// Draws the connecting line on top of the area fill for a series.
    /// </summary>
    private static void DrawConnectingLine(ICanvas canvas, IList<Point> coordinates, Color color, float lineWidth)
    {
        if (coordinates.Count < 2) return;

        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            var p1 = coordinates[i];
            var p2 = coordinates[i + 1];
            canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, lineWidth);
        }
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's AreaColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => AreaColor;
}