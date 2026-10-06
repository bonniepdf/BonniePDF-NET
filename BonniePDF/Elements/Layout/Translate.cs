using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Fluent;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout;

/// <summary>Moves native painted content without changing its original flow box.</summary>
public class Translate : ElementBase, IContentContainer, IStatefulElement
{
    /// <summary>The native subtree to transform.</summary>
    public IElement? Child { get; set; }
    private float _x, _y;
    /// <summary>Signed horizontal paint offset; positive moves right.</summary>
    public float X { get => _x; set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _x = value; } }
    /// <summary>Signed vertical paint offset; positive moves down.</summary>
    public float Y { get => _y; set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _y = value; } }
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
        AtomicTransform.RenderChild(Child, ctx, w, h, h, new PaintTransform(1, 0, 0, 1, X, Y));
    }
}
