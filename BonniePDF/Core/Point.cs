using System.Runtime.InteropServices;

namespace BonniePDF;

/// <summary>
/// Represents a 2D point with X and Y coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Point(float x, float y)
{
    /// <summary>Horizontal coordinate, in points.</summary>
    public float X = x;
    /// <summary>Vertical coordinate, in points.</summary>
    public float Y = y;
}