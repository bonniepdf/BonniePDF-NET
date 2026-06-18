using BonniePDF.Charts.Models;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Charts;

/// <summary>
/// Custom element that renders a chart using ICanvas to PDF.
/// </summary>
public class ChartElement : ElementBase, IRoleProvider, IAltTextProvider
{
    private readonly ChartBase _chart;
    private readonly ChartData _data;

    public ChartElement(ChartBase chart, ChartData data)
    {
        _chart = chart;
        _data = data;
    }

    /// <summary>
    /// Alternate text description for screen readers (e.g. "Bar chart showing monthly sales").
    /// When set on a PDF/UA-1 document, the chart is tagged as a /Figure with this alt text.
    /// When empty the chart is tagged as /Artifact (decorative), matching the TS ChartElement
    /// behaviour when no altText is provided.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c> the chart is unconditionally tagged as /Artifact regardless of AltText.
    /// </summary>
    public bool IsDecorative { get; set; }

    /// <inheritdoc />
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? SemanticRole.Artifact
            : SemanticRole.Figure;

    protected override float GetPreferredContentWidth()
    {
        return _chart.Width;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        // If stretching to fit, use container width for measurement
        if (_chart.StretchToFit) return _chart.Height;
        return _chart.Height;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        // If StretchToFit is enabled, set the chart width to match the container
        if (_chart.StretchToFit) _chart.Width = contentWidth;

        // Get a canvas instance configured for the chart's position
        // This handles both PDF and SVG modes seamlessly
        ICanvas canvas = ctx.GetCanvas(ctx.CurrentX, ctx.CurrentY);

        // Draw the chart using the ICanvas interface
        _chart.Draw(canvas, _data);

        // Advance Y position by the chart height
        ctx.CurrentY += _chart.Height;
    }
}