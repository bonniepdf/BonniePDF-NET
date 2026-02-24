namespace BonniePDF.Models;

/// <summary>
/// Represents a 2D point with X and Y coordinates
/// </summary>
public struct Point(float x, float y)
{
    public float X = x;
    public float Y = y;
}