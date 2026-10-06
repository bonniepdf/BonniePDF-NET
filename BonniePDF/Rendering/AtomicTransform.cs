using System;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Rendering;

internal static class AtomicTransform
{
    internal static void ResetChild(IElement? child) { if (child is IStatefulElement stateful) stateful.ResetState(); }
    internal static void Render(IElement element, RenderContext ctx, float width, Action draw)
    {
        float x = ctx.CurrentX, y = ctx.CurrentY;
        if (element.MeasureHeight(width) > ctx.ContentHeight + 0.001f) throw new InvalidOperationException("Transformed element is taller than the page content band.");
        try { draw(); }
        catch { ctx.CurrentX = x; ctx.CurrentY = y; ctx.LayoutRecorder?.AbortNode(element); throw; }
    }
    internal static RenderStatus RenderStateful(IElement element, RenderContext ctx, float width, Action draw)
    {
        float height = element.MeasureHeight(width);
        if (height > ctx.ContentHeight + 0.001f) throw new InvalidOperationException("Transformed element is taller than the page content band.");
        if (height > ctx.MaxY - ctx.CurrentY + 0.001f) return RenderStatus.WrapRequired;
        draw(); return RenderStatus.Complete;
    }
    internal static void RenderChild(IElement? child, RenderContext ctx, float width, float height, float flowHeight, PaintTransform transform)
    {
        if (child is null) return;
        float x = ctx.CurrentX, y = ctx.CurrentY;
        var band = ctx.AtomicLayoutBounds;
        var previous = ctx.ElementTransform;
        var target = ctx.CellStretchTarget;
        float stretch = ctx.CellStretchHeight;
        var recorder = ctx.LayoutRecorder;
        var previousCapture = recorder?.ElementTransform;
        bool success = false;
        ResetChild(child);
        bool saved = false, tagged = false;
        ChildTagToken tag = default;
        try
        {
            if (!transform.IsIdentity)
            {
                ctx.Canvas.SaveState();
                saved = true;
                if (ctx.Canvas is PdfCanvas pdf) pdf.TransformElement(transform);
                else ctx.Canvas.Transform(transform.A, transform.B, transform.C, transform.D, transform.E, transform.F);
            }
            ctx.AtomicLayoutBounds = (y, y + height + 0.001f);
            ctx.ElementTransform = ElementTransforms.Compose(previous, transform);
            if (recorder is not null) recorder.ElementTransform = ctx.ElementTransform;
            ctx.CellStretchTarget = null;
            ctx.CellStretchHeight = 0;
            recorder?.OpenNode(child, LayoutRecorder.CaptureTypeOf(child), LayoutRecorder.CaptureNameOf(child), ctx.CurrentPageIndex, x, y, width);
            tag = ChildTagging.BeginChildTag(ctx, child);
            tagged = true;
            if (child is IStatefulElement stateful)
            {
                if (stateful.RenderStateful(ctx, width) != RenderStatus.Complete) throw new InvalidOperationException("Transformed content cannot continue onto another page.");
            }
            else child.Render(ctx, width);
            if (ctx.CurrentY == y) ctx.CurrentY += height;
            recorder?.CloseNode(child, ctx.CurrentPageIndex, ctx.CurrentY, complete: true);
            success = true;
        }
        finally
        {
            if (tagged) ChildTagging.EndChildTag(ctx, tag);
            ctx.AtomicLayoutBounds = band;
            ctx.ElementTransform = previous;
            if (recorder is not null) recorder.ElementTransform = previousCapture;
            ctx.CellStretchTarget = target;
            ctx.CellStretchHeight = stretch;
            ctx.CurrentX = x;
            ctx.CurrentY = success ? y + flowHeight : y;
            if (saved) ctx.Canvas.RestoreState();
            ResetChild(child);
        }
    }
}
