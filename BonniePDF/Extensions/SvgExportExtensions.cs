using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Rendering;

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
    /// Note: Chart SVG export uses standard font fallback rendering without per-glyph kerning.
    /// For accurate kerning with embedded fonts, render the chart via a Page using ToSvg(Page) instead.
    /// </summary>
    public static string ToSvg(this ChartBase chart, ChartData data)
    {
        // Charts use standard fonts internally, so no per-glyph font resolver is needed here.
        SvgCanvas canvas = new(chart.Width, chart.Height);

        // Draw the chart
        chart.Draw(canvas, data);

        // Finalize SVG
        return canvas.GetSvg();
    }

    /// <summary>
    /// Renders a 1D barcode to a standalone SVG string using the same vector
    /// element renderer as PDF output.
    /// </summary>
    public static string ToSvg(this Barcode barcode)
    {
        return ElementToSvg(barcode);
    }

    /// <summary>
    /// Renders a QR code to a standalone SVG string using the same vector
    /// element renderer as PDF output.
    /// </summary>
    public static string ToSvg(this QRCode qrCode)
    {
        return ElementToSvg(qrCode);
    }

    /// <summary>
    /// Renders a Data Matrix symbol to a standalone SVG string using the same
    /// vector element renderer as PDF output.
    /// </summary>
    public static string ToSvg(this DataMatrix dataMatrix)
    {
        return ElementToSvg(dataMatrix);
    }

    private static string ElementToSvg(IElement element)
    {
        float width = element.GetPreferredWidth();
        float height = element.MeasureHeight(width);
        RenderContext ctx = new(new PdfSize(width, height), 0, true);

        element.Render(ctx, width);

        if (ctx.Canvas is SvgCanvas svgCanvas) return svgCanvas.GetSvg();

        return ctx.CurrentStream.ToString();
    }
}
