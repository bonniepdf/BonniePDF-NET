using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Fluent;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout;

/// <summary>How rotation affects the flow box.</summary>
public enum RotationMode
{
    /// <summary>Keep the unrotated flow box and rotate about its centre.</summary>
    Free,
    /// <summary>Swap dimensions for quarter turns and keep paint inside the flow box.</summary>
    Constrained
}

/// <summary>Rotates a native subtree clockwise without rasterising its content.</summary>
public class Rotate : ElementBase, IContentContainer, IStatefulElement
{
    /// <summary>The subtree to rotate.</summary>
    public IElement? Child { get; set; }
    /// <summary>Finite clockwise degrees.</summary>
    public float Angle { get; set; }
    /// <summary>Free angles or constrained multiples of 90 degrees.</summary>
    public RotationMode Mode { get; set; }
    private float? _contentWidth;
    /// <summary>Optional positive width of the child before rotation, in points.</summary>
    public float? ContentWidth
    {
        get => _contentWidth;
        set
        {
            if (value is float w && (!float.IsFinite(w) || w <= 0)) throw new ArgumentOutOfRangeException(nameof(value), "Rotate contentWidth must be positive and finite.");
            _contentWidth = value;
        }
    }
    /// <summary>Traverse the native child for fonts, styles and accessibility.</summary>
    public IEnumerable<IElement> GetChildren() => Child is null ? [] : [Child];
    /// <summary>Reset the child for a fresh atomic render.</summary>
    public void ResetState()
    {
        if (Child is IStatefulElement stateful) stateful.ResetState();
    }
    private (float Angle, float Width, float Height, float FlowHeight) Geometry(float width)
    {
        if (!float.IsFinite(Angle)) throw new InvalidOperationException("Rotate angle must be finite.");
        if (Mode is not RotationMode.Free and not RotationMode.Constrained) throw new InvalidOperationException("Invalid rotation mode.");
        float angle = ((Angle % 360) + 360) % 360;
        if (Mode == RotationMode.Constrained && angle % 90 != 0) throw new InvalidOperationException("Constrained rotation requires a multiple of 90 degrees.");
        bool quarter = Mode == RotationMode.Constrained && angle is 90 or 270;
        float preferred = Child?.GetPreferredWidth() ?? 0;
        float w = ContentWidth ?? (quarter && preferred > 0 ? preferred : width);
        float h = Child?.MeasureHeight(w) ?? 0;
        return (angle, w, h, Child is null ? 0 : quarter ? w : h);
    }
    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) => Geometry(contentWidth).FlowHeight;
    /// <inheritdoc />
    protected override float GetPreferredContentWidth()
    {
        if (Child is null) return 0;
        float width = ContentWidth ?? Child.GetPreferredWidth();
        var g = Geometry(width);
        return Mode == RotationMode.Constrained && g.Angle is 90 or 270 ? g.Height : width;
    }
    /// <summary>Render atomically, restoring the context if the child fails.</summary>
    public new void Render(RenderContext ctx, float width) => AtomicTransform.Render(this, ctx, width, () => base.Render(ctx, width));
    /// <summary>Move to a fresh page before painting when the whole rotated box does not fit.</summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width) => AtomicTransform.RenderStateful(this, ctx, width, () => Render(ctx, width));
    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        var g = Geometry(contentWidth);
        if (Child is null) return;
        float x = ctx.CurrentX, y = ctx.CurrentY;
        double radians = g.Angle * Math.PI / 180;
        float a = g.Angle switch { 0 => 1, 90 => 0, 180 => -1, 270 => 0, _ => (float)Math.Round(Math.Cos(radians), 6, MidpointRounding.AwayFromZero) };
        float b = g.Angle switch { 0 => 0, 90 => 1, 180 => 0, 270 => -1, _ => (float)Math.Round(Math.Sin(radians), 6, MidpointRounding.AwayFromZero) };
        float cx = x + g.Width / 2, cy = y + g.Height / 2;
        float e = cx - a * cx + b * cy, f = cy - b * cx - a * cy;
        if (Mode == RotationMode.Constrained)
        {
            e = g.Angle switch { 90 => x + y + g.Height, 270 => x - y, 180 => 2 * x + g.Width, _ => 0 };
            f = g.Angle switch { 90 => y - x, 270 => y + x + g.Width, 180 => 2 * y + g.Height, _ => 0 };
        }
        var transform = new PaintTransform(a, b, -b, a, e, f);
        AtomicTransform.RenderChild(Child, ctx, g.Width, g.Height, g.FlowHeight, transform);
    }
}
