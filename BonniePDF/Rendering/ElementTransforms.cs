using BonniePDF.Drawing;

namespace BonniePDF.Rendering;

internal static class ElementTransforms
{
    internal static PaintTransform Compose(PaintTransform? parent, PaintTransform c)
    {
        var p = parent ?? PaintTransform.Identity;
        return new(p.A * c.A + p.C * c.B, p.B * c.A + p.D * c.B,
            p.A * c.C + p.C * c.D, p.B * c.C + p.D * c.D,
            p.A * c.E + p.C * c.F + p.E, p.B * c.E + p.D * c.F + p.F);
    }
}
