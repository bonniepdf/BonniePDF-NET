// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Globalization;
namespace BonniePDF;

internal readonly record struct NiceScaleResult(decimal MinValue, decimal MaxValue, decimal StepSize, int StepCount, string AutoFormat);

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

    private static double NiceNum(double range, bool round)
    {
        double exp = Math.Floor(Math.Log10(range));
        double pow = Math.Pow(10, exp);
        double frac = range / pow;
        double nf = round
            ? (frac < 1.5 ? 1 : frac < 3 ? 2 : frac < 7 ? 5 : 10)
            : (frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 5 ? 5 : 10);
        return nf * pow;
    }

    private static double CleanDouble(double value)
    {
        if (Math.Abs(value) < 1e-12) return 0;
        return double.Parse(value.ToString("G12", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    internal static NiceScaleResult ComputeNiceScale(
        double dataMin,
        double dataMax,
        int targetSteps,
        bool includeZero,
        double headroom,
        double? explicitMin = null,
        double? explicitMax = null)
    {
        double lo = includeZero ? Math.Min(0, dataMin) : dataMin;
        double hi = includeZero ? Math.Max(0, dataMax) : dataMax;
        if (explicitMin.HasValue) lo = explicitMin.Value;
        if (explicitMax.HasValue) hi = explicitMax.Value;

        if (lo == hi)
        {
            if (lo == 0)
            {
                lo = 0;
                hi = 1;
            }
            else
            {
                double pad = Math.Abs(lo) * 0.1;
                lo -= pad;
                hi += pad;
                if (includeZero)
                {
                    if (!explicitMin.HasValue) lo = Math.Min(0, lo);
                    if (!explicitMax.HasValue) hi = Math.Max(0, hi);
                }
            }
        }

        double h = double.IsFinite(headroom) ? Math.Clamp(headroom, 0, 0.5) : 0;
        if (h > 0)
        {
            double span = hi - lo;
            if (!explicitMax.HasValue && hi > 0 && dataMax >= hi - 1e-12) hi += span * h;
            if (!explicitMin.HasValue && lo < 0 && dataMin <= lo + 1e-12) lo -= span * h;
        }

        int steps = Math.Max(2, targetSteps <= 0 ? 5 : targetSteps);
        double niceRange = NiceNum(hi - lo, false);
        double tickSpacing = CleanDouble(NiceNum(niceRange / steps, true));
        const double eps = 1e-9;

        double minValue = explicitMin ?? CleanDouble(Math.Floor(lo / tickSpacing + eps) * tickSpacing);
        double maxValue = explicitMax ?? CleanDouble(Math.Ceiling(hi / tickSpacing - eps) * tickSpacing);
        if (maxValue <= minValue) maxValue = CleanDouble(minValue + tickSpacing);

        int stepCount = Math.Max(1, (int)Math.Round((maxValue - minValue) / tickSpacing, MidpointRounding.AwayFromZero));
        int decimals = Math.Clamp(-(int)Math.Floor(Math.Log10(tickSpacing) + eps), 0, 6);
        return new NiceScaleResult(
            (decimal)minValue,
            (decimal)maxValue,
            (decimal)tickSpacing,
            stepCount,
            string.Create(CultureInfo.InvariantCulture, $"N{decimals}"));
    }

    private static double SrgbLinear(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    /// <summary>WCAG 2.1 relative luminance contrast selection: <c>#ffffff</c> or <c>#0f172a</c>.</summary>
    internal static Color ContrastTextColor(Color background)
    {
        double lum = 0.2126 * SrgbLinear(background.ScR) + 0.7152 * SrgbLinear(background.ScG) + 0.0722 * SrgbLinear(background.ScB);
        return lum < 0.1791 ? Color.White : Color.FromHex("#0f172a");
    }

    /// <summary>Blends a possibly translucent colour over an opaque background.</summary>
    internal static Color BlendOverOpaque(Color foreground, Color background)
    {
        float a = foreground.ScA;
        if (a >= 1f) return foreground;
        byte r = (byte)Math.Clamp((int)Math.Round((foreground.ScR * a + background.ScR * (1f - a)) * 255f, MidpointRounding.AwayFromZero), 0, 255);
        byte g = (byte)Math.Clamp((int)Math.Round((foreground.ScG * a + background.ScG * (1f - a)) * 255f, MidpointRounding.AwayFromZero), 0, 255);
        byte b = (byte)Math.Clamp((int)Math.Round((foreground.ScB * a + background.ScB * (1f - a)) * 255f, MidpointRounding.AwayFromZero), 0, 255);
        return new Color(r, g, b, (byte)255);
    }
}
