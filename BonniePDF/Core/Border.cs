using System.Runtime.InteropServices;

namespace BonniePDF;

/// <summary>
/// Represents border widths for the four sides of an element.
/// All values are in points (1/72 inch).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Border
{
    /// <summary>Border width on the left side, in points.</summary>
    public float Left { get; set; }
    /// <summary>Border width on the top side, in points.</summary>
    public float Top { get; set; }
    /// <summary>Border width on the right side, in points.</summary>
    public float Right { get; set; }
    /// <summary>Border width on the bottom side, in points.</summary>
    public float Bottom { get; set; }

    /// <summary>Creates uniform border width on all sides</summary>
    public Border(float width)
    {
        Left = Top = Right = Bottom = width;
    }

    /// <summary>Creates individual border widths for each side</summary>
    public Border(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>Creates border with only left side</summary>
    public static Border LeftOnly(float width)
    {
        return new Border(width, 0, 0, 0);
    }

    /// <summary>Creates border with only right side</summary>
    public static Border RightOnly(float width)
    {
        return new Border(0, 0, width, 0);
    }

    /// <summary>Creates border with only top side</summary>
    public static Border TopOnly(float width)
    {
        return new Border(0, width, 0, 0);
    }

    /// <summary>Creates border with only bottom side</summary>
    public static Border BottomOnly(float width)
    {
        return new Border(0, 0, 0, width);
    }

    /// <summary>Creates border with vertical sides only (left and right)</summary>
    public static Border VerticalSides(float width)
    {
        return new Border(width, 0, width, 0);
    }

    /// <summary>Creates border with horizontal sides only (top and bottom)</summary>
    public static Border HorizontalSides(float width)
    {
        return new Border(0, width, 0, width);
    }

    /// <summary>No border (all sides zero)</summary>
    public static Border None => new(0);

    /// <summary>Checks if any side has a border width greater than zero</summary>
    public bool HasAnyBorder => Left > 0 || Top > 0 || Right > 0 || Bottom > 0;

    /// <summary>Total horizontal border width (left + right)</summary>
    public float HorizontalWidth => Left + Right;

    /// <summary>Total vertical border width (top + bottom)</summary>
    public float VerticalWidth => Top + Bottom;
}