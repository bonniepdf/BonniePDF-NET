using System.Runtime.InteropServices;

namespace BonniePDF;

/// <summary>
/// Represents padding or margin values for the four sides of an element
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Thickness
{
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
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