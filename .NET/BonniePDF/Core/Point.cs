using System.Runtime.InteropServices;

namespace BonniePDF.Core;

/// <summary>
/// Represents a 2D point with X and Y coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Point(float x, float y)
{
    public float X = x;
    public float Y = y;
}