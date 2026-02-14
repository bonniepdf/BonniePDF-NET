using BonniePDF.Charts;
using BonniePDF.Drawing;
using BonniePDF.Layout;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;

namespace BonniePDF.Extensions;

/// <summary>
/// Extension methods for exporting Pages and Charts to SVG.
/// </summary>
public static class SvgExportExtensions
{
    /// <summary>
    /// Renders the page to an SVG string.
    /// </summary>
    public static string ToSvg(this Page page)
    {
        // Create RenderContext in SVG mode
        RenderContext ctx = new(new PdfSize(page.Width, page.Height), page.Margin, true);

        // Mimic the layout loop from PdfRenderPipeline
        foreach (IElement element in page.Elements)
        {
            // Measure height to ensure consistent layout behavior (some elements might cache calculations)
            float elementHeight = element.MeasureHeight(page.ContentWidth);

            // Render the element
            element.Render(ctx, page.ContentWidth);
        }

        // Get the underlying SvgCanvas to finalize the output
        if (ctx.Canvas is SvgCanvas svgCanvas) return svgCanvas.GetSvg();

        return ctx.CurrentStream.ToString();
    }

    /// <summary>
    /// Renders the chart to an SVG string.
    /// </summary>
    public static string ToSvg(this ChartBase chart, ChartData data)
    {
        // Create canvas that writes to its own internal StringBuilder (will include header/footer)
        SvgCanvas canvas = new(chart.Width, chart.Height);

        // Draw the chart
        chart.Draw(canvas, data);

        // Finalize SVG
        return canvas.GetSvg();
    }
}