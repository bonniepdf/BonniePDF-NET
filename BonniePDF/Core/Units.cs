namespace BonniePDF.Core;

/// <summary>
/// Provides unit conversion utilities for measurements
/// </summary>
public static class Units
{
    private const float PointsPerInch = 72f;
    private const float PointsPerCentimeter = 72f / 2.54f;
    private const float PointsPerMillimeter = 72f / 25.4f;

    /// <summary>
    /// Converts a value from the specified unit to points
    /// </summary>
    public static float ToPoints(float value, Unit unit)
    {
        return unit switch
        {
            Unit.Point => value,
            Unit.Inch => value * PointsPerInch,
            Unit.Centimeter => value * PointsPerCentimeter,
            Unit.Millimeter => value * PointsPerMillimeter,
            _ => value
        };
    }

    /// <summary>Converts inches to points</summary>
    public static float Inch(float value)
    {
        return value * PointsPerInch;
    }

    /// <summary>Converts centimeters to points</summary>
    public static float Cm(float value)
    {
        return value * PointsPerCentimeter;
    }

    /// <summary>Converts millimeters to points</summary>
    public static float Mm(float value)
    {
        return value * PointsPerMillimeter;
    }
}