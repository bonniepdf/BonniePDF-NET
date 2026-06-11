using System;
using System.Collections.Generic;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// An inline z-stacking primitive. All layers share the same origin and
/// paint in declaration order: <b>background</b> layers render behind, the
/// <b>primary</b> layer defines the element's flow height, and
/// <b>foreground</b> layers render on top. This gives clean visual stacking
/// inside any container - card backgrounds behind a label, badges over an
/// image, and so on - without the absolute-positioning workarounds.
/// </summary>
public class Layers : ElementBase
{
    private readonly List<IElement> _background = new();
    private readonly List<IElement> _foreground = new();

    /// <summary>
    /// The layer that determines the flow height of the stack. When null, the
    /// tallest declared layer defines the height instead.
    /// </summary>
    public IElement PrimaryLayer { get; set; }

    /// <summary>Layers painted behind the primary layer, in declaration order.</summary>
    public IReadOnlyList<IElement> BackgroundLayers => _background.AsReadOnly();

    /// <summary>Layers painted on top of the primary layer, in declaration order.</summary>
    public IReadOnlyList<IElement> ForegroundLayers => _foreground.AsReadOnly();

    /// <summary>Adds a layer painted behind the primary layer. Fluent; returns this.</summary>
    public Layers AddBackground(IElement element)
    {
        if (element != null) _background.Add(element);
        return this;
    }

    /// <summary>Adds a layer painted on top of the primary layer. Fluent; returns this.</summary>
    public Layers AddForeground(IElement element)
    {
        if (element != null) _foreground.Add(element);
        return this;
    }

    protected override float GetPreferredContentWidth()
    {
        return PrimaryLayer?.GetPreferredWidth() ?? 0;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        if (PrimaryLayer != null) return PrimaryLayer.MeasureHeight(contentWidth);

        // No primary layer: the tallest layer defines the height.
        float max = 0f;
        foreach (IElement layer in _background) max = MathF.Max(max, layer.MeasureHeight(contentWidth));
        foreach (IElement layer in _foreground) max = MathF.Max(max, layer.MeasureHeight(contentWidth));
        return max;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        float startX = ctx.CurrentX;
        float startY = ctx.CurrentY;

        // Background layers: paint at the shared origin without committing Y.
        foreach (IElement layer in _background)
            RenderOverlay(ctx, layer, startX, startY, contentWidth);

        // Primary layer: defines the flow - it advances CurrentY normally.
        float endY;
        if (PrimaryLayer != null)
        {
            ctx.CurrentX = startX;
            ctx.CurrentY = startY;
            ctx.Canvas.SaveState();
            try { PrimaryLayer.Render(ctx, contentWidth); }
            finally { ctx.Canvas.RestoreState(); }
            endY = ctx.CurrentY;
        }
        else
        {
            endY = startY + MeasureContentHeight(contentWidth);
        }

        // Foreground layers: paint at the shared origin, on top of the primary.
        foreach (IElement layer in _foreground)
            RenderOverlay(ctx, layer, startX, startY, contentWidth);

        ctx.CurrentX = startX;
        ctx.CurrentY = endY;
    }

    private static void RenderOverlay(RenderContext ctx, IElement layer, float startX, float startY, float contentWidth)
    {
        ctx.CurrentX = startX;
        ctx.CurrentY = startY;
        ctx.Canvas.SaveState();
        try { layer.Render(ctx, contentWidth); }
        finally { ctx.Canvas.RestoreState(); }
    }
}
