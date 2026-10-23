using System.Runtime.InteropServices;

namespace BonniePDF;

/// <summary>
/// Represents a 2D size with width and height
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Size(float w, float h)
{
    /// <summary>Width, in points.</summary>
    public float Width = w;
    /// <summary>Height, in points.</summary>
    public float Height = h;
}