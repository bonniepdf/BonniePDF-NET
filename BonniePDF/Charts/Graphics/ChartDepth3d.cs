// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
namespace BonniePDF;

/// <summary>Oblique depth for bars: each bar gains a top face and a right face, projected at <see cref="Angle"/>. Two-dimensional charts are easier to read accurately.</summary>
public sealed class ChartDepth3d
{
    /// <summary>Depth of the bars in points (<c>12</c>).</summary>
    public float Depth { get; set; } = 12;
    /// <summary>The projection angle in degrees, above 0 and below 90 (<c>45</c>).</summary>
    public float Angle { get; set; } = 45;
    /// <summary>Draw a back wall and floor behind the bars, with the gridlines on the back wall (<see langword="false"/>).</summary>
    public bool Walls { get; set; }

    /// <summary>Validates the depth and returns how far a point at full depth moves right and up.</summary>
    internal (float Dx, float Dy) Resolve()
    {
        if (!(Depth > 0) || !float.IsFinite(Depth)) throw new InvalidOperationException("depth3d depth must be positive.");
        if (!(Angle > 0 && Angle < 90)) throw new InvalidOperationException("depth3d angle must be above 0 and below 90 degrees.");
        double t = Angle * Math.PI / 180;
        return ((float)(Depth * Math.Cos(t)), (float)(Depth * Math.Sin(t)));
    }
}
