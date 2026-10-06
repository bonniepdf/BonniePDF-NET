using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Fluent;

namespace BonniePDF;

/// <summary>Mirrors a native subtree about its box centre without changing flow dimensions.</summary>
public class Flip : ElementBase, IContentContainer, IStatefulElement
{
    /// <summary>The native subtree to transform.</summary>
    public IElement? Child { get; set; }
    /// <summary>Mirror left and right about the child box centre.</summary>
    public bool Horizontal { get; set; }
    /// <summary>Mirror top and bottom about the child box centre.</summary>
    public bool Vertical { get; set; }
    private float? _contentWidth;
    /// <summary>Optional positive child width before transformation, in points.</summary>
    public float? ContentWidth
    {
        get => _contentWidth;
        set
        {
            if (value is float width && (!float.IsFinite(width) || width <= 0)) throw new ArgumentOutOfRangeException(nameof(value));
            _contentWidth = value;
        }
    }
    /// <summary>Traverse the child for fonts, styles and accessibility.</summary>
    public IEnumerable<IElement> GetChildren() => Child is null ? [] : [Child];
    /// <summary>Reset the child for a fresh atomic render.</summary>
    public void ResetState() => AtomicTransform.ResetChild(Child);
    /// <inheritdoc />
    protected override float GetPreferredContentWidth() => Child is null ? 0 : ContentWidth ?? Child.GetPreferredWidth();
    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) => Child?.MeasureHeight(ContentWidth ?? contentWidth) ?? 0;
    /// <summary>Render atomically and restore failed layout.</summary>
    public new void Render(RenderContext ctx, float width) => AtomicTransform.Render(this, ctx, width, () => base.Render(ctx, width));
    /// <summary>Move whole to a fresh page before painting when necessary.</summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width) => AtomicTransform.RenderStateful(this, ctx, width, () => Render(ctx, width));
    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        float w = ContentWidth ?? contentWidth, h = Child?.MeasureHeight(w) ?? 0;
        AtomicTransform.RenderChild(Child, ctx, w, h, h, new PaintTransform(Horizontal ? -1 : 1, 0, 0, Vertical ? -1 : 1, Horizontal ? 2 * ctx.CurrentX + w : 0, Vertical ? 2 * ctx.CurrentY + h : 0));
    }
}
