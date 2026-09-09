using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace BonniePDF.Compliance;

// ---------------------------------------------------------------------------
// WCAG 2.2 contrast maths.
//
// Pure, dependency-free helpers shared by the compliance preflight. Everything
// here is defined by WCAG 2.2 (SC 1.4.3) for the sRGB colour space, which is the
// space BonniePDF's Color already stores.
//
// Deliberately small: the preflight's character is "pure, deterministic,
// dependency-light", and contrast does not need more than this.
//
// Mirrors Contrast.ts in the TypeScript core; both conform to
// the specification §4.3.
//
// NOTE: authored without a .NET toolchain in the build environment — must be
// compiled and run against the shared fixtures before it is considered landed.
// ---------------------------------------------------------------------------

/// <summary>Why a backdrop could not be reduced to a single opaque colour.</summary>
public enum ContrastUnknownReason
{
    /// <summary>An ancestor paints layered or freely drawn content.</summary>
    UnmodelledBackdrop,
    /// <summary>The page paints a background element rather than a flat colour.</summary>
    PageBackdrop,
    /// <summary>The text or the resolved backdrop is not fully opaque; compositing is not modelled.</summary>
    Translucent
}

/// <summary>The outcome of comparing a text colour to its resolved backdrop.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ContrastVerdict
{
    /// <summary>True when the backdrop could not be resolved; <see cref="Reason"/> says why.</summary>
    public bool IsIndeterminate { get; init; }
    /// <summary>True when the measured ratio meets the required one. Meaningless when indeterminate.</summary>
    public bool Passed { get; init; }
    /// <summary>Measured ratio, rounded to 2 decimal places. Meaningless when indeterminate.</summary>
    public double Ratio { get; init; }
    /// <summary>The ratio required at this text size. Meaningless when indeterminate.</summary>
    public double Required { get; init; }
    /// <summary>Why the backdrop was unresolvable. Meaningful only when indeterminate.</summary>
    public ContrastUnknownReason Reason { get; init; }

    public static ContrastVerdict Indeterminate(ContrastUnknownReason reason) =>
        new() { IsIndeterminate = true, Reason = reason };

    public static ContrastVerdict Measured(double ratio, double required) =>
        new() { Passed = ratio >= required, Ratio = ratio, Required = required };
}

/// <summary>WCAG 2.2 relative luminance and contrast-ratio maths for sRGB colours.</summary>
public static class Contrast
{
    /// <summary>
    /// Parses a "#rrggbb" string into 0-255 channels. Returns false for any value
    /// that is not exactly six hex digits after the hash, so a malformed snapshot
    /// degrades to "unknown" rather than to a wrong verdict.
    /// </summary>
    public static bool TryParseHexRgb(string? hex, out (byte R, byte G, byte B) rgb)
    {
        rgb = default;
        if (hex is null || hex.Length != 7 || hex[0] != '#') return false;

        for (int i = 1; i < 7; i++)
        {
            char c = hex[i];
            bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }

        rgb = (
            byte.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>One sRGB channel (0-255) linearised per WCAG 2.2.</summary>
    private static double Linearise(byte channel)
    {
        double c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// WCAG 2.2 relative luminance of an sRGB colour, in [0, 1].
    /// Black is 0, white is 1.
    /// </summary>
    public static double RelativeLuminance((byte R, byte G, byte B) rgb) =>
        0.2126 * Linearise(rgb.R) + 0.7152 * Linearise(rgb.G) + 0.0722 * Linearise(rgb.B);

    /// <summary>
    /// WCAG 2.2 contrast ratio between two sRGB colours, in [1, 21].
    /// Order-independent: the lighter colour is always the numerator.
    /// </summary>
    public static double Ratio((byte R, byte G, byte B) a, (byte R, byte G, byte B) b)
    {
        double la = RelativeLuminance(a);
        double lb = RelativeLuminance(b);
        double lighter = Math.Max(la, lb);
        double darker = Math.Min(la, lb);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// The WCAG 2.2 AA ratio required for text of this size, per SC 1.4.3.
    ///
    /// Sizes are in <em>points</em>, which is PDF's native unit — the CSS px
    /// conversion (18pt = 24px, 14pt bold = 18.66px) does not apply here.
    /// </summary>
    public static double RequiredRatio(float? fontSize, bool bold)
    {
        float size = fontSize ?? 0f;
        bool large = size >= 18f || (size >= 14f && bold);
        return large ? 3.0 : 4.5;
    }

    /// <summary>Rounds to 2 decimal places, so both implementations agree on borderline cases.</summary>
    public static double Round(double ratio) => Math.Round(ratio, 2, MidpointRounding.AwayFromZero);
}
