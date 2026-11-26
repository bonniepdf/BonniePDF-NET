// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
namespace BonniePDF;

/// <summary>A tilted pie: the disc is drawn as an ellipse and the slices gain side walls. Flat pies are easier to read accurately.</summary>
public sealed class ChartPie3d
{
    /// <summary>How far the pie leans back, in degrees from <c>0</c> to <c>80</c> (<c>50</c>). The vertical radius is the radius times the cosine of the tilt.</summary>
    public float Tilt { get; set; } = 50;
    /// <summary>The height of the side walls in points (<c>16</c>), before the tilt foreshortens it: a wall appears <c>Depth · sin(Tilt)</c> tall.</summary>
    public float Depth { get; set; } = 16;
    /// <summary>Write each slice's share inside it when <c>SliceLabelMode</c> is <c>none</c> (<see langword="true"/>), because projected slices are hard to compare by eye.</summary>
    public bool Labels { get; set; } = true;

    /// <summary>The largest number of slices a tilted pie may have.</summary>
    internal const int MaxSlices = 8;

    /// <summary>Validates the tilt and depth and returns the vertical radius as a fraction of the horizontal one, and the wall height as drawn.</summary>
    internal (double YScale, float Depth) Resolve()
    {
        if (!float.IsFinite(Tilt) || Tilt < 0 || Tilt > 80) throw new InvalidOperationException("pie3d tilt must be from 0 to 80 degrees.");
        if (!float.IsFinite(Depth) || Depth < 0) throw new InvalidOperationException("pie3d depth must be finite and nonnegative.");
        // The pie is a cylinder seen from above at an angle: the disc's vertical radius shrinks by cos(tilt), and the vertical
        // walls appear sin(tilt) of their height. Flat on (tilt 0) the walls vanish; edge on they would show in full.
        double t = Tilt * Math.PI / 180;
        return (Math.Cos(t), (float)(Depth * Math.Sin(t)));
    }
}
