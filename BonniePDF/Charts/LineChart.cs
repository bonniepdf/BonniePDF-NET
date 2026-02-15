using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Line chart that renders connected data points with straight lines.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class LineChart : CartesianChartBase
{
    private const string LineColorHex = "#4f46e5";
    private const float DataPointRadius = 5;

    /// <summary>
    /// Color for the line and data points
    /// </summary>
    public Color LineColor { get; set; } = Color.FromHex(LineColorHex);

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
    /// Whether to show circular markers at data points
    /// </summary>
    public bool ShowDataPointMarkers { get; set; } = true;

    /// <summary>
    /// Draws the line chart elements (connecting line and optional data point markers).
    /// </summary>
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Draw the connecting line
        DrawConnectingLine(canvas, coordinates);

        // Draw data point markers if enabled
        if (ShowDataPointMarkers)
            DrawDataPointMarkers(canvas, coordinates);
    }

    /// <summary>
    /// Draws value labels above each data point.
    /// </summary>
    protected override void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
        if (!ShowValueLabels) return;

        Color labelColor = LineColor;

        for (int i = 0; i < data.DataPoints.Count; i++)
        {
            (float x, float y) = coordinates[i];
            string text = data.DataPoints[i].FormattedValue;

            // Draw text above the data point
            canvas.DrawText(x, y - DataPointRadius - 7 - LabelFontSize, text, labelColor, LabelFontSize, FontFamily,
                TextAlignment.Center);
        }
    }

    /// <summary>
    /// Draws the connecting line between all data points.
    /// </summary>
    private void DrawConnectingLine(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        if (coordinates.Count < 2) return;

        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            (float x1, float y1) = coordinates[i];
            (float x2, float y2) = coordinates[i + 1];

            canvas.DrawLine(x1, y1, x2, y2, LineColor, LineWidth);
        }
    }

    /// <summary>
    /// Draws circular markers at each data point.
    /// </summary>
    private void DrawDataPointMarkers(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        foreach ((float x, float y) in coordinates) canvas.FillCircle(x, y, DataPointRadius, LineColor);
    }
}