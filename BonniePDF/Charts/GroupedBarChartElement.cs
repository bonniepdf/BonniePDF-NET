using BonniePDF.Charts.Models;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;
using System;

namespace BonniePDF.Charts;

/// <summary>
/// Element that renders a GroupedBarChart to the document.
/// </summary>
[Obsolete("GroupedBarChart is a document element. Set its Data and add it directly.")]
public class GroupedBarChartElement : ElementBase, IRoleProvider, IAltTextProvider
{
    private readonly GroupedBarChart _chart;
    private readonly ChartData _data;

    /// <summary>
    /// Creates a new GroupedBarChartElement.
    /// </summary>
    /// <param name="chart">The grouped bar chart to render</param>
    /// <param name="data">The chart data containing series, axis options, and title</param>
    public GroupedBarChartElement(GroupedBarChart chart, ChartData data)
    {
        _chart = chart;
        _data = data;
    }

    /// <summary>
    /// Alternate text description for screen readers (e.g. "Grouped bar chart comparing monthly revenue").
    /// When set on a PDF/UA-1 document, the chart is tagged as a /Figure with this alt text.
    /// When empty the chart is tagged as /Artifact (decorative), matching <see cref="ChartElement"/>.
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

    /// <summary>
    /// Measures the height of the chart content.
    /// </summary>
    protected override float MeasureContentHeight(float contentWidth)
    {
        return _chart.Height;
    }

    /// <summary>
    /// Renders the grouped bar chart to the document.
    /// </summary>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        // If StretchToFit is enabled, set the chart width to match the container
        if (_chart.StretchToFit) _chart.Width = contentWidth;

        // Get a canvas via ctx to ensure fonts/images are properly registered
        ICanvas canvas = ctx.GetCanvas(ctx.CurrentX, ctx.CurrentY);

        // Draw the chart using the ICanvas interface
        _chart.Draw(canvas, _data);

        // Advance Y position by the chart height
        ctx.CurrentY += _chart.Height;
    }
}
