using BonniePDF.Core;

namespace BonniePDF.Drawing;

/// <summary>Path fill rule.</summary>
public enum FillRule
{
    /// <summary>Non-zero winding number rule.</summary>
    NonZero,

    /// <summary>Even-odd rule.</summary>
    EvenOdd
}

/// <summary>Line cap style for stroke endpoints.</summary>
public enum LineCap
{
    /// <summary>Butt cap (square end at the endpoint).</summary>
    Butt,

    /// <summary>Round cap (semicircle at the endpoint).</summary>
    Round,

    /// <summary>Square cap (extends half the line width beyond the endpoint).</summary>
    Square
}

/// <summary>Line join style for stroke corners.</summary>
public enum LineJoin
{
    /// <summary>Miter join (sharp corner, clipped at miter limit).</summary>
    Miter,

    /// <summary>Round join (circular arc at the corner).</summary>
    Round,

    /// <summary>Bevel join (straight line across the corner).</summary>
    Bevel
}

/// <summary>Stroke parameters for path drawing.</summary>
public sealed class StrokeStyle
{
    /// <summary>Gets or initializes the paint used for the stroke.</summary>
    public Paint Paint { get; init; } = new SolidPaint(Color.Black);

    /// <summary>Gets or initializes the stroke width in points.</summary>
    public float Width { get; init; } = 1f;

    /// <summary>Gets or initializes the dash pattern (alternating on/off lengths).</summary>
    public float[]? DashPattern { get; init; }

    /// <summary>Gets or initializes the dash phase offset.</summary>
    public float DashOffset { get; init; }

    /// <summary>Gets or initializes the line cap style.</summary>
    public LineCap LineCap { get; init; } = LineCap.Butt;

    /// <summary>Gets or initializes the line join style.</summary>
    public LineJoin LineJoin { get; init; } = LineJoin.Miter;

    /// <summary>Gets or initializes the miter limit.</summary>
    public float MiterLimit { get; init; } = 10f;
}
