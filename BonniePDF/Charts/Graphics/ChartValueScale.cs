// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Globalization;
namespace BonniePDF;

/// <summary>
/// The linear value axis shared by every cartesian chart. A fraction is 0 at <c>min</c> and 1 at
/// <c>max</c>, measured from the plot's bottom edge (vertical) or left edge (horizontal), and is not clamped.
/// </summary>
internal static class ChartValueScale
{
    /// <summary>Throws unless a chart with series can be drawn on this range.</summary>
    internal static void Check(decimal min, decimal max)
    {
        if (max <= min)
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                $"The value axis needs finite bounds with MaxValue greater than MinValue (got {min} to {max})."));
    }

    /// <summary>The fraction of the plot at which <paramref name="value"/> sits.</summary>
    internal static decimal Fraction(decimal value, decimal min, decimal max) => (value - min) / (max - min);

    /// <summary>The value at which bars, area fills and stems start: zero, limited to the range.</summary>
    private static decimal OriginValue(decimal min, decimal max) => Math.Min(Math.Max(0m, min), max);

    /// <summary>The fraction at which bars, area fills and stems start.</summary>
    internal static decimal OriginFraction(decimal min, decimal max) => Fraction(OriginValue(min, max), min, max);

    /// <summary>Whether <paramref name="value"/> lies below the origin, so its label goes below (or left of) its point.</summary>
    internal static bool IsBelowOrigin(decimal value, decimal min, decimal max) => value < OriginValue(min, max);
}
