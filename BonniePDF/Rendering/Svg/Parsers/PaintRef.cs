// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>What a <c>fill</c> or <c>stroke</c> resolved to.</summary>
internal enum PaintRefKind
{
    /// <summary>A concrete colour.</summary>
    Color,

    /// <summary>Do not paint at all.</summary>
    None,

    /// <summary>Defer to the <c>color</c> property in effect.</summary>
    CurrentColor,

    /// <summary>A <c>url(#id)</c> reference; outside this profile.</summary>
    Reference
}

/// <summary>
/// A resolved paint reference.
/// </summary>
/// <remarks>
/// <see cref="PaintRefKind.None"/> is distinct from a transparent colour: it means
/// "do not paint", which skips stroking entirely rather than stroking invisibly,
/// and it changes how <c>inherit</c> behaves.
/// </remarks>
/// <param name="Kind">Which case this is.</param>
/// <param name="Color">The colour, when <see cref="Kind"/> is <see cref="PaintRefKind.Color"/>.</param>
/// <param name="ReferenceId">The referenced id, when <see cref="Kind"/> is <see cref="PaintRefKind.Reference"/>.</param>
internal readonly record struct PaintRef(PaintRefKind Kind, SinkColor Color = default, string? ReferenceId = null)
{
    /// <summary>A solid colour paint.</summary>
    public static PaintRef FromColor(SinkColor color) => new(PaintRefKind.Color, color);

    /// <summary>The "do not paint" value.</summary>
    public static readonly PaintRef None = new(PaintRefKind.None);
}

/// <summary>Parses SVG paint values.</summary>
internal static partial class ColorParser
{
    /// <summary>
    /// Parses any SVG paint value, returning null for anything unrecognised so
    /// the cascade can fall back. A malformed colour in one icon should not fail
    /// a whole document.
    /// </summary>
    public static PaintRef? Parse(string input)
    {
        string text = input.Trim();
        if (text.Length == 0) return null;
        string lower = text.ToLowerInvariant();

        if (string.Equals(lower, "none", StringComparison.Ordinal)) return PaintRef.None;
        if (string.Equals(lower, "transparent", StringComparison.Ordinal)) return PaintRef.FromColor(new SinkColor(0, 0, 0, 0));
        if (string.Equals(lower, "currentcolor", StringComparison.Ordinal)) return new PaintRef(PaintRefKind.CurrentColor);

        if (lower.StartsWith("url(", StringComparison.Ordinal))
        {
            var match = UrlReference().Match(text);
            return match.Success ? new PaintRef(PaintRefKind.Reference, default, match.Groups["id"].Value) : null;
        }

        if (text.StartsWith('#')) return ParseHex(text[1..]);

        var functional = FunctionalColor().Match(text);
        if (functional.Success) return ParseFunctional(functional.Groups["name"].Value.ToLowerInvariant(), functional.Groups["body"].Value);

        return NamedColors.TryGet(lower, out int rgb)
            ? PaintRef.FromColor(FromBytes((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255))
            : null;
    }

    private static PaintRef? ParseHex(string hex)
    {
        static bool IsHex(string value) => value.All(Uri.IsHexDigit);

        // 3 and 4 digit forms double each digit: #abc is #aabbcc.
        if (hex.Length is 3 or 4 && IsHex(hex))
        {
            int[] parts = hex.Select(c => Convert.ToInt32(new string(c, 2), 16)).ToArray();
            double alpha = parts.Length == 4 ? parts[3] / 255.0 : 1.0;
            return PaintRef.FromColor(FromBytes(parts[0], parts[1], parts[2], alpha));
        }

        if (hex.Length == 6 && IsHex(hex))
        {
            int value = Convert.ToInt32(hex, 16);
            return PaintRef.FromColor(FromBytes((value >> 16) & 255, (value >> 8) & 255, value & 255));
        }

        if (hex.Length == 8 && IsHex(hex))
        {
            long value = Convert.ToInt64(hex, 16);
            return PaintRef.FromColor(FromBytes(
                (int)((value >> 24) & 255), (int)((value >> 16) & 255), (int)((value >> 8) & 255),
                (value & 255) / 255.0));
        }

        return null;
    }

    [GeneratedRegex(@"^url\([ \t\n\r\f]*['""]?#(?<id>[^'"") \t\n\r\f]+)['""]?[ \t\n\r\f]*\)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlReference();

    [GeneratedRegex(@"^(?<name>rgba?|hsla?)\((?<body>[^)]*)\)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FunctionalColor();

    private static readonly char[] FunctionalSeparators = [' ', ',', '\t', '\n', '\r', '\f'];

    private static PaintRef? ParseFunctional(string name, string body)
    {
        // Both comma and space separated forms are legal; the space form uses a
        // slash before alpha: rgb(1 2 3 / 0.5).
        // Only the first slash, as the TypeScript engine does: a second one is
        // malformed, and leaves a part that fails to parse.
        int slash = body.IndexOf('/', StringComparison.Ordinal);
        if (slash >= 0) body = string.Concat(body.AsSpan(0, slash), " ", body.AsSpan(slash + 1));
        string[] parts = body.Split(FunctionalSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;

        if (name.StartsWith("rgb", StringComparison.Ordinal))
        {
            double alpha = parts.Length > 3 ? AlphaChannel(parts[3]) : 1.0;
            return PaintRef.FromColor(FromChannels(Channel(parts[0]), Channel(parts[1]), Channel(parts[2]), alpha));
        }

        double hue = Single(parts[0]);
        double saturation = Single(parts[1].TrimEnd('%')) / 100.0;
        double lightness = Single(parts[2].TrimEnd('%')) / 100.0;
        double hslAlpha = parts.Length > 3 ? AlphaChannel(parts[3]) : 1.0;
        (double r, double g, double b) = HslToRgb(hue, saturation, lightness);
        return PaintRef.FromColor(FromChannels(r, g, b, hslAlpha));
    }

    private static double Single(string text) =>
        double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>A channel may be a number 0-255 or a percentage.</summary>
    private static double Channel(string text)
    {
        string trimmed = text.Trim();
        return trimmed.EndsWith('%') ? Single(trimmed[..^1]) / 100.0 * 255.0 : Single(trimmed);
    }

    private static double AlphaChannel(string text)
    {
        string trimmed = text.Trim();
        return trimmed.EndsWith('%') ? Single(trimmed[..^1]) / 100.0 : Single(trimmed);
    }

    // Channels are computed in double and narrowed once here. SinkColor is a
    // sink-seam type and mirrors ICanvas, so it stays float.
    private static SinkColor FromBytes(int r, int g, int b, double a = 1.0) =>
        new((float)Clamp01(r / 255.0), (float)Clamp01(g / 255.0), (float)Clamp01(b / 255.0), (float)Clamp01(a));

    private static SinkColor FromChannels(double r, double g, double b, double a) =>
        new((float)Clamp01(r / 255.0), (float)Clamp01(g / 255.0), (float)Clamp01(b / 255.0), (float)Clamp01(a));

    private static double Clamp01(double value) => value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;

    /// <summary>CSS Color 3 HSL-to-RGB. Hue wraps; saturation and lightness clamp.</summary>
    private static (double, double, double) HslToRgb(double hueDegrees, double s, double l)
    {
        double h = ((hueDegrees % 360.0) + 360.0) % 360.0 / 360.0;
        double saturation = Clamp01(s);
        double lightness = Clamp01(l);

        if (saturation == 0.0)
        {
            double grey = lightness * 255.0;
            return (grey, grey, grey);
        }

        double q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
        double p = 2 * lightness - q;
        return (HueToChannel(p, q, h + 1.0 / 3.0) * 255.0, HueToChannel(p, q, h) * 255.0, HueToChannel(p, q, h - 1.0 / 3.0) * 255.0);
    }

    private static double HueToChannel(double p, double q, double tIn)
    {
        double t = tIn;
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }
}
