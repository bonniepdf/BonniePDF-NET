using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Text;

namespace BonniePDF.Charts;

/// <summary>
/// Base class for all charts that provides common functionality for drawing charts
/// using the ICanvas interface. This ensures charts can be rendered to any backend
/// (PDF, SVG, etc.) without modification.
/// </summary>
public abstract class ChartBase
{
    // Title styling constants (matching SVG chart implementation)
    private const float TitleFontSize = 20;
    private const float TitleY = 35;
    private const string TitleFontFamily = "Arial, sans-serif";
    private static readonly Color TitleColor = Color.FromHex("#333333");

    /// <summary>
    /// Width of the chart canvas in points
    /// </summary>
    public float Width { get; set; } = 800;

    /// <summary>
    /// Height of the chart canvas in points
    /// </summary>
    public float Height { get; set; } = 450;

    /// <summary>
    /// Background color for the chart (default: null/transparent).
    /// When null, no background is drawn, allowing the chart to blend with document content.
    /// Set to Color.White for standalone chart images.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// When true, the chart will stretch to fill the container width.
    /// The Width property will be set automatically by the ChartElement.
    /// </summary>
    public bool StretchToFit { get; set; }

    /// <summary>
    /// Preferred root <c>shape-rendering</c> for standalone SVG export
    /// (<see cref="Extensions.SvgExportExtensions.ToSvg(ChartBase, Models.ChartData)"/>).
    /// <c>null</c> (the default) keeps the SVG default antialiasing, which is
    /// required for charts with diagonal lines, arcs, or circles. Axis-aligned
    /// charts (bar, grouped bar) set this to <c>"crispEdges"</c> so their bars
    /// and gridlines snap to the pixel grid instead of softening into blurred
    /// edges. Text is governed by <c>text-rendering</c>, so it is unaffected.
    /// </summary>
    public string? SvgShapeRendering { get; set; }

    /// <summary>
    /// Draws the chart to the specified canvas.
    /// This method handles the common setup (background, title) and calls the abstract DrawContent method.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="data">The chart data to render</param>
    public virtual void Draw(ICanvas canvas, ChartData data)
    {
        // Draw background only if a color is specified
        if (BackgroundColor.HasValue) canvas.FillRect(0, 0, Width, Height, BackgroundColor.Value);

        // Draw title if present
        if (!string.IsNullOrEmpty(data.Title)) DrawTitle(canvas, data.Title);

        // Draw chart-specific content
        DrawContent(canvas, data);
    }

    /// <summary>
    /// Draws the chart title centered at the top.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="title">The title text</param>
    protected virtual void DrawTitle(ICanvas canvas, string title)
    {
        float centerX = Width / 2;
        canvas.DrawText(centerX, TitleY, title, TitleColor, TitleFontSize, TitleFontFamily, TextAlignment.Center);
    }

    /// <summary>
    /// Draws the chart-specific content.
    /// Derived classes must implement this to provide their specific drawing logic.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="data">The chart data to render</param>
    protected abstract void DrawContent(ICanvas canvas, ChartData data);
}