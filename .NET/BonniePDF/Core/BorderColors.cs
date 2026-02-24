namespace BonniePDF.Core;

/// <summary>
/// Represents border colors for the four sides of an element
/// </summary>
public struct BorderColors
{
    public Color? Left { get; set; }
    public Color? Top { get; set; }
    public Color? Right { get; set; }
    public Color? Bottom { get; set; }

    /// <summary>Creates uniform border color on all sides</summary>
    public BorderColors(Color color)
    {
        Left = Top = Right = Bottom = color;
    }

    /// <summary>Creates individual border colors for each side</summary>
    public BorderColors(Color? left, Color? top, Color? right, Color? bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>Creates border color with only left side</summary>
    public static BorderColors LeftOnly(Color color)
    {
        return new BorderColors(color, null, null, null);
    }

    /// <summary>Creates border color with only right side</summary>
    public static BorderColors RightOnly(Color color)
    {
        return new BorderColors(null, null, color, null);
    }

    /// <summary>Creates border color with only top side</summary>
    public static BorderColors TopOnly(Color color)
    {
        return new BorderColors(null, color, null, null);
    }

    /// <summary>Creates border color with only bottom side</summary>
    public static BorderColors BottomOnly(Color color)
    {
        return new BorderColors(null, null, null, color);
    }

    /// <summary>Creates border color with vertical sides only</summary>
    public static BorderColors VerticalSides(Color color)
    {
        return new BorderColors(color, null, color, null);
    }

    /// <summary>Creates border color with horizontal sides only</summary>
    public static BorderColors HorizontalSides(Color color)
    {
        return new BorderColors(null, color, null, color);
    }

    /// <summary>Checks if any side has a border color</summary>
    public bool HasAnyColor => Left.HasValue || Top.HasValue || Right.HasValue || Bottom.HasValue;
}