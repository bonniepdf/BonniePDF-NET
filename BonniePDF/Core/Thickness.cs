using System.Runtime.InteropServices;

namespace BonniePDF;

/// <summary>
/// Represents padding or margin values for the four sides of an element
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Thickness
{
    /// <summary>Thickness on the left side, in points.</summary>
    public float Left { get; set; }
    /// <summary>Thickness on the top side, in points.</summary>
    public float Top { get; set; }
    /// <summary>Thickness on the right side, in points.</summary>
    public float Right { get; set; }
    /// <summary>Thickness on the bottom side, in points.</summary>
    public float Bottom { get; set; }

    /// <summary>Creates uniform thickness on all sides</summary>
    public Thickness(float u)
    {
        Left = Top = Right = Bottom = u;
    }

    /// <summary>Creates horizontal and vertical thickness</summary>
    public Thickness(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    /// <summary>Creates individual thickness for each side</summary>
    public Thickness(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}