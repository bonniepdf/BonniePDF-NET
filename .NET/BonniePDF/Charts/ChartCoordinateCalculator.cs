namespace BonniePDF.Charts;

/// <summary>
/// Provides coordinate calculation utilities for chart rendering.
/// All coordinates are in Screen Coordinates (Top-Left origin).
/// </summary>
public static class ChartCoordinateCalculator
{
    private const int FirstDataPointX = 95;
    private const int DataPointSpacing = 58;
    private const int BaselineY = 352;
    private const int ChartHeight = 292; // Distance from y=60 to y=352

    /// <summary>
    /// Calculates the X coordinate for a data point index.
    /// </summary>
    public static int CalculateX(int index)
    {
        return FirstDataPointX + index * DataPointSpacing;
    }

    /// <summary>
    /// Calculates the Y coordinate for a given value.
    /// </summary>
    public static int CalculateY(decimal value, decimal maxValue)
    {
        if (maxValue == 0) return BaselineY;

        decimal normalizedValue = value / maxValue;
        int y = BaselineY - (int)(normalizedValue * ChartHeight);
        return y;
    }

    /// <summary>
    /// Calculates bezier control points for smooth curved lines.
    /// </summary>
    public static ((int x, int y) cp1, (int x, int y) cp2) CalculateBezierControlPoints(int x1, int y1, int x2, int y2)
    {
        // Calculate control points for smooth bezier curves
        // Using a simple approach: control points are at 1/3 and 2/3 of the way
        int midX = (x1 + x2) / 2;
        int cp1x = x1 + (midX - x1) / 2;
        int cp1y = y1;
        int cp2x = midX + (x2 - midX) / 2;
        int cp2y = y2;

        return ((cp1x, cp1y), (cp2x, cp2y));
    }
}