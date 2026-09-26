// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>The context a relative length resolves against.</summary>
/// <param name="PercentBasis">The dimension a percentage is relative to.</param>
/// <param name="FontSize">Font size in user units, for <c>em</c>.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct LengthContext(double PercentBasis, double FontSize)
{
    /// <summary>The default: no percentage basis, a 16 unit font size.</summary>
    public static readonly LengthContext Default = new(0, 16);
}

/// <summary>Resolves SVG lengths to user units.</summary>
internal static partial class LengthParser
{
    /// <summary>Units per CSS pixel. SVG user units are CSS pixels at 96dpi.</summary>
    private static readonly Dictionary<string, double> Units = new(StringComparer.Ordinal)
    {
        [""] = 1,
        ["px"] = 1,
        ["pt"] = 96.0 / 72.0,
        ["pc"] = 16,
        ["in"] = 96,
        ["cm"] = 96 / 2.54,
        ["mm"] = 96 / 25.4,
        ["q"] = 96 / 25.4 / 4,
    };

    [GeneratedRegex(@"^(?<number>[+-]?(?:[0-9]*\.[0-9]+|[0-9]+\.?)(?:[eE][+-]?[0-9]+)?)[ \t\n\r\f]*(?<unit>[a-z%]*)$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Pattern();

    /// <summary>
    /// Resolves a length, returning null for anything unrecognised so the caller
    /// can fall back to a default rather than aborting the document.
    /// </summary>
    public static double? Parse(string input, LengthContext? context = null)
    {
        LengthContext resolved = context ?? LengthContext.Default;
        string text = input.Trim();
        if (text.Length == 0) return null;

        Match match = Pattern().Match(text);
        if (!match.Success) return null;

        if (!double.TryParse(match.Groups["number"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || !double.IsFinite(value))
        {
            throw new SvgParseException("length is not a finite number", 0, text);
        }

        string unit = match.Groups["unit"].Value.ToLowerInvariant();
        if (string.Equals(unit, "%", StringComparison.Ordinal)) return value / 100 * resolved.PercentBasis;
        if (string.Equals(unit, "em", StringComparison.Ordinal)) return value * resolved.FontSize;
        // `ex` needs font metrics; half the em is the conventional approximation,
        // documented as such rather than presented as exact.
        if (string.Equals(unit, "ex", StringComparison.Ordinal)) return value * resolved.FontSize * 0.5;

        return Units.TryGetValue(unit, out double scale) ? value * scale : null;
    }
}
