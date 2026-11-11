using System;
using System.Globalization;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Draws content that cannot split, handling the case where it is taller than the body band (spec 02 §4.3).
/// </summary>
/// <remarks>
/// Callers move such content to a fresh page first, so content that still overruns the band here fits on no page.
/// It is drawn from where it stands, clipped to the band, and reported once at error severity; with
/// <see cref="OversizedContent.Throw"/> generation fails instead, before anything is drawn. Content drawn inside an
/// oversized box shares its clip and is not reported again.
/// </remarks>
internal static class OversizedContentPlacement
{
    internal static void Draw(RenderContext ctx, object element, float height, Action draw) =>
        Draw(ctx, LayoutRecorder.CaptureTypeOf(element), height, draw);

    internal static void Draw(RenderContext ctx, string type, float height, Action draw)
    {
        double overrun = (double)ctx.CurrentY + height - ctx.MaxY;
        if (overrun <= 0.01 || ctx.OversizedClipDepth > 0)
        {
            draw();
            return;
        }

        string message = string.Create(CultureInfo.InvariantCulture,
            $"A {type} is taller than the page body and cannot be split: {Math.Round(overrun, MidpointRounding.AwayFromZero):0}pt is cut off at the bottom of page {ctx.CurrentPageIndex + 1}.");
        if (ctx.Resources.OversizedContent == OversizedContent.Throw) throw new OversizedContentException(message);
        ctx.Resources.Diagnostics.AddError(DiagnosticCategory.Render, message, "pagination");

        ICanvas canvas = ctx.Canvas;
        canvas.SaveState();
        // Four explicit sides rather than AddRect, whose operators differ between the two implementations' canvases.
        canvas.BeginPath();
        canvas.MoveTo(0, 0);
        canvas.LineTo(ctx.PageSize.Width, 0);
        canvas.LineTo(ctx.PageSize.Width, ctx.MaxY);
        canvas.LineTo(0, ctx.MaxY);
        canvas.ClosePath();
        canvas.ClipToCurrentPath();
        ctx.OversizedClipDepth++;
        try
        {
            draw();
        }
        finally
        {
            ctx.OversizedClipDepth--;
            canvas.RestoreState();
        }
    }
}
