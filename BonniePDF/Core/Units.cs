using System;
using System.Globalization;

namespace BonniePDF;

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

    /// <summary>
    /// Parses a unit-suffixed length token - <c>"1in"</c>, <c>"10mm"</c>,
    /// <c>"2.5cm"</c>, <c>"72pt"</c> - into points.
    /// </summary>
    /// <remarks>
    /// The same grammar the TypeScript implementation accepts wherever a
    /// measurement is written, offered here for configuration-driven and
    /// round-tripped values. Whitespace around the number and before the unit is
    /// allowed; the unit is case-insensitive. Prefer the numeric extension
    /// methods (<c>1.Inches()</c>) when the value is a literal in code.
    /// </remarks>
    /// <exception cref="FormatException">The token is not a valid length.</exception>
    public static float Parse(string token)
    {
        if (!TryParse(token, out float points))
        {
            throw new FormatException(
                $"Invalid length \"{token}\". Write a number of points (72), " +
                "or a unit token (\"1in\", \"10mm\", \"2.5cm\", \"72pt\").");
        }

        return points;
    }

    /// <summary>
    /// Parses a unit-suffixed length token into points, returning <c>false</c>
    /// rather than throwing when the token is not well-formed.
    /// </summary>
    public static bool TryParse(string? token, out float points)
    {
        points = 0f;
        if (string.IsNullOrWhiteSpace(token)) return false;

        ReadOnlySpan<char> span = token.AsSpan().Trim();

        int split = span.Length;
        while (split > 0 && char.IsLetter(span[split - 1])) split--;
        if (split == 0 || split == span.Length) return false;

        ReadOnlySpan<char> suffix = span[split..];
        ReadOnlySpan<char> number = span[..split].TrimEnd();
        if (number.IsEmpty) return false;

        Unit unit;
        if (suffix.Equals("pt", StringComparison.OrdinalIgnoreCase)) unit = Unit.Point;
        else if (suffix.Equals("in", StringComparison.OrdinalIgnoreCase)) unit = Unit.Inch;
        else if (suffix.Equals("cm", StringComparison.OrdinalIgnoreCase)) unit = Unit.Centimeter;
        else if (suffix.Equals("mm", StringComparison.OrdinalIgnoreCase)) unit = Unit.Millimeter;
        else return false;

        if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return false;

        points = ToPoints(value, unit);
        return true;
    }
}