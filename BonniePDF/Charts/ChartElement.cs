using BonniePDF.Charts.Models;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Rendering;

namespace BonniePDF.Charts;

/// <summary>
/// Custom element that renders a chart using ICanvas to PDF.
/// </summary>
public class ChartElement : ElementBase
{
    private readonly ChartBase _chart;
    private readonly ChartData _data;

    public ChartElement(ChartBase chart, ChartData data)
    {
        _chart = chart;
        _data = data;
    }

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