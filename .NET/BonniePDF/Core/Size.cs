namespace BonniePDF.Core;

/// <summary>
/// Represents a 2D size with width and height
/// </summary>
public struct Size(float w, float h)
{
    public float Width = w;
    public float Height = h;
}