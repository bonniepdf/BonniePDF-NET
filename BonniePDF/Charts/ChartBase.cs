// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Base class for all charts that provides common functionality for drawing charts
/// using the ICanvas interface. This ensures charts can be rendered to any backend
/// (PDF, SVG, etc.) without modification.
/// </summary>
public abstract partial class ChartBase
{
    private string _theme = "legacy";
    /// <summary>Appearance preset: "legacy" (default) or "modern".</summary>
    public string Theme { get => _theme; set { if (value is not "legacy" and not "modern") throw new ArgumentException("Invalid chart theme.", nameof(value)); _theme = value; } }
    /// <summary>The values and behaviours behind <see cref="Theme"/>.</summary>
    internal ChartTheme Appearance => ChartTheme.For(Theme);
    internal virtual bool UsesStyledGeometry => Appearance.StyledGeometry;

    /// <summary>Creates an empty chart for construct-then-mutate authoring.</summary>
    protected ChartBase()
    {
    }

    /// <summary>Creates a chart bound to the data it will render.</summary>
    protected ChartBase(ChartData data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    private ChartData _data = new();

    /// <summary>Data rendered by this chart when it is used as a document element.</summary>
    public ChartData Data
    {
        get => _data;
        set => _data = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Visible title drawn inside the chart.</summary>
    public string Title
    {
        get => Data.Title;
        set => Data.Title = value;
    }

    /// <summary>Alternate text description for screen readers.</summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>Marks the chart as decorative even when alternate text is present.</summary>
    public bool IsDecorative { get; set; }

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
    /// Preferred root <c>shape-rendering</c> for standalone SVG export
    /// (<see cref="SvgExportExtensions.ToSvg(ChartBase, ChartData)"/>).
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

    /// <summary>Draws the chart's owned <see cref="Data"/> to a canvas.</summary>
    public void Draw(ICanvas canvas) => Draw(canvas, Data);

    /// <summary>
    /// Draws the chart title in the theme's title style.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="title">The title text</param>
    protected virtual void DrawTitle(ICanvas canvas, string title)
    {
        ChartTheme t = Appearance;
        canvas.DrawText(t.TitleX ?? Width / 2, t.TitleY, title, t.TitleColor, t.TitleFontSize, t.TitleFontFamily, t.TitleAlignment);
    }

    /// <summary>
    /// Draws the chart-specific content.
    /// Derived classes must implement this to provide their specific drawing logic.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="data">The chart data to render</param>
    protected abstract void DrawContent(ICanvas canvas, ChartData data);

}
