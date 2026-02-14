using BonniePDF.Charts;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

/// <summary>
/// Element that renders a GroupedBarChart to the document.
/// </summary>
public class GroupedBarChartElement : ElementBase
{
    private readonly GroupedBarChart _chart;

    /// <summary>
    /// Creates a new GroupedBarChartElement.
    /// </summary>
    /// <param name="chart">The grouped bar chart to render</param>
    public GroupedBarChartElement(GroupedBarChart chart)
    {
        _chart = chart;
    }

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

        // Create a PdfCanvas that wraps the current PDF content stream
        PdfCanvas pdfCanvas = new(ctx.CurrentStream, ctx.PageHeight, ctx.CurrentX, ctx.CurrentY);

        // Create a dummy ChartData for the Draw method (won't be used for grouped bars)
        ChartData dummyData = new();

        // Draw the chart using the ICanvas interface
        _chart.Draw(pdfCanvas, dummyData);

        // Advance Y position by the chart height
        ctx.CurrentY += _chart.Height;
    }
}