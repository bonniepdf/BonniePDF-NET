using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;

namespace BonniePDF.Charts;

/// <summary>
/// Lollipop chart that renders vertical lines (stems) with circular markers (heads).
/// Now supports multiple series for comparing datasets.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class LollipopChart : CartesianChartBase
{
    private const string DefaultColorHex = "#4f46e5";
    private const float DefaultHeadRadiusValue = 8;

    /// <summary>
    /// Default stem width for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultStemWidth { get; set; } = 2f;

    /// <summary>
    /// Default head radius for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultHeadRadius { get; set; } = DefaultHeadRadiusValue;

    /// <summary>
    /// Color for the lollipop stems and heads (backward compatibility).
    /// </summary>
    public Color LollipopColor { get; set; } = Color.FromHex(DefaultColorHex);

    /// <summary>
    /// Width of the stem line (backward compatibility).
    /// </summary>
    public float StemWidth { get; set; } = 2f;

    /// <summary>
    /// Radius of the lollipop head (circle) (backward compatibility).
    /// </summary>
    public float HeadRadius { get; set; } = DefaultHeadRadiusValue;

    /// <summary>
    /// Draws the lollipop chart elements for a single series (stems and heads).
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Get series-specific styling or use defaults
        float stemWidth = series.Style?.StemWidth > 0 ? series.Style.StemWidth : DefaultStemWidth;
        float headRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultHeadRadius;

        // Draw lollipop stems (vertical lines)
        DrawLollipopStems(canvas, coordinates, baselineY, series.Color, stemWidth);

        // Draw lollipop heads (circles)
        DrawLollipopHeads(canvas, coordinates, series.Color, headRadius);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (accounts for lollipop head radius).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float headRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultHeadRadius;
        return headRadius + 7;
    }

    /// <summary>
    /// Draws the lollipop stems (vertical lines from baseline to data point) for a series.
    /// </summary>
    private void DrawLollipopStems(ICanvas canvas, List<(float x, float y)> coordinates, 
        float baselineY, Color color, float stemWidth)
    {
        foreach ((float x, float y) in coordinates)
            canvas.DrawLine(x, baselineY, x, y, color, stemWidth);
    }

    /// <summary>
    /// Draws the lollipop heads (circles at data points) for a series.
    /// </summary>
    private void DrawLollipopHeads(ICanvas canvas, List<(float x, float y)> coordinates, Color color, float radius)
    {
        foreach ((float x, float y) in coordinates)
            canvas.FillCircle(x, y, radius, color);
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's LollipopColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => LollipopColor;

    #region Backward Compatibility - Obsolete Methods

    /// <summary>
    /// Draws the lollipop chart elements (stems and heads).
    /// OBSOLETE: Use DrawSeriesElements instead for multi-series support.
    /// </summary>
    [System.Obsolete("Use DrawSeriesElements instead for multi-series support")]
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // For backward compatibility, treat as single series
        var series = data.Series.Count > 0 ? data.Series[0] : new ChartSeries { Color = LollipopColor };
        DrawSeriesElements(canvas, data, series, coordinates, chartAreaWidth, chartAreaHeight, baselineY, topY);
    }

    #endregion
}
