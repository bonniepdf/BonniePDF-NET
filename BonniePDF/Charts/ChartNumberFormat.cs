using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace BonniePDF.Charts;

/// <summary>
/// Number formatting for chart text: data-point labels, the pie total and the axis ticks.
/// </summary>
/// <remarks>
/// One small grammar, culture-invariant (<c>.</c> for decimals, <c>,</c> for thousands, always),
/// computed on the value's decimal digits so a .NET <see cref="decimal"/> and a TypeScript
/// <c>number</c> produce the same text:
/// <list type="table">
/// <item><term><c>G</c> (default)</term><description>the shortest decimal, plain notation, no grouping</description></item>
/// <item><term><c>F&lt;n&gt;</c></term><description>fixed-point, <c>n</c> decimals (<c>F</c> alone is <c>F2</c>)</description></item>
/// <item><term><c>N&lt;n&gt;</c></term><description>fixed-point, <c>n</c> decimals, grouped in thousands (<c>N</c> alone is <c>N2</c>)</description></item>
/// <item><term><c>P&lt;n&gt;</c></term><description>the value times 100, <c>n</c> decimals, grouped, then <c>%</c> (<c>P</c> alone is <c>P2</c>)</description></item>
/// </list>
/// <c>n</c> is 0-15. Rounding is half away from zero. A negative result that rounds to zero is
/// written without its sign. Anything else throws <see cref="FormatException"/>. The current
/// culture is never consulted.
/// </remarks>
public static partial class ChartNumberFormat
{
    private const int MaxDecimals = 15;

    [GeneratedRegex("^(?<kind>[GFNPgfnp])(?<n>[0-9]{1,2})?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FormatPattern();

    private static (char Kind, int Decimals)? Parse(string? format)
    {
        if (string.IsNullOrEmpty(format)) return ('G', 0);
        Match match = FormatPattern().Match(format);
        if (!match.Success) return null;
        char kind = char.ToUpperInvariant(match.Groups["kind"].Value[0]);
        Group digitGroup = match.Groups["n"];
        if (kind == 'G') return digitGroup.Success ? null : ('G', 0);
        int decimals = digitGroup.Success ? int.Parse(digitGroup.Value, CultureInfo.InvariantCulture) : 2;
        return decimals <= MaxDecimals ? (kind, decimals) : null;
    }

    /// <summary>True when <paramref name="format"/> is in the chart number grammar (null and empty mean <c>G</c>).</summary>
    public static bool IsSupported(string? format) => Parse(format) is not null;

    /// <summary>
    /// Formats <paramref name="value"/> with <paramref name="format"/>.
    /// </summary>
    /// <exception cref="FormatException">The format is outside the grammar.</exception>
    public static string Format(decimal value, string? format = null)
    {
        (char kind, int decimals) = Parse(format)
            ?? throw new FormatException(
                $"Unsupported chart number format '{format}'. Use G, F<n>, N<n> or P<n>, with n from 0 to {MaxDecimals}.");

        // The decimal's own digits, in plain notation, never the current culture's.
        string text = value.ToString(CultureInfo.InvariantCulture);
        bool negative = text.StartsWith('-');
        if (negative) text = text[1..];
        int point = text.IndexOf('.', StringComparison.Ordinal);
        string digits = point < 0 ? text : text.Remove(point, 1);
        int scale = point < 0 ? 0 : text.Length - point - 1;

        // Same normal form as the other implementation: no trailing zeros in the fraction.
        digits = digits.TrimStart('0');
        if (digits.Length == 0) digits = "0";
        while (scale > 0 && digits.EndsWith('0'))
        {
            digits = digits[..^1];
            scale--;
        }
        if (string.Equals(digits, "0", StringComparison.Ordinal))
        {
            negative = false;
            scale = 0;
        }

        if (kind == 'P')
        {
            if (scale >= 2) scale -= 2;
            else
            {
                digits += new string('0', 2 - scale);
                scale = 0;
            }
        }

        if (kind != 'G')
        {
            int n = decimals;
            if (scale > n)
            {
                int cut = scale - n;
                string padded = digits.PadLeft(cut + 1, '0');
                string kept = padded[..^cut];
                char next = padded[padded.Length - cut];
                digits = next >= '5' ? (BigInteger.Parse(kept, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture) : kept;
            }
            else
            {
                digits += new string('0', n - scale);
            }
            scale = n;
        }

        string body = digits.PadLeft(scale + 1, '0');
        string whole = scale == 0 ? body : body[..^scale];
        string fraction = scale == 0 ? "" : body[^scale..];
        if (kind == 'G') fraction = fraction.TrimEnd('0');
        if (kind is 'N' or 'P') whole = Group(whole);

        bool isZero = whole.Replace(",", "", StringComparison.Ordinal).Trim('0').Length == 0 && fraction.Trim('0').Length == 0;
        var result = new StringBuilder();
        if (negative && !isZero) result.Append('-');
        result.Append(whole);
        if (fraction.Length > 0) result.Append('.').Append(fraction);
        if (kind == 'P') result.Append('%');
        return result.ToString();
    }

    private static string Group(string whole)
    {
        var grouped = new StringBuilder();
        for (int i = 0; i < whole.Length; i++)
        {
            if (i > 0 && (whole.Length - i) % 3 == 0) grouped.Append(',');
            grouped.Append(whole[i]);
        }
        return grouped.ToString();
    }
}
