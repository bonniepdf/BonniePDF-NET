// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace BonniePDF;

/// <summary>
/// Number formatting for chart text: data-point labels, the pie total and the axis ticks.
/// </summary>
public static partial class ChartNumberFormat
{
    private const int MaxDecimals = 15;
    private static readonly string[] CompactSuffixes = ["", "K", "M", "B", "T"];

    [GeneratedRegex("^(?<kind>[GFNPKgfnpk])(?<n>[0-9]{1,2})?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FormatPattern();

    private static (char Kind, int Decimals, bool ExplicitDecimals)? Parse(string? format)
    {
        if (string.IsNullOrEmpty(format)) return ('G', 0, false);
        Match match = FormatPattern().Match(format);
        if (!match.Success) return null;
        char kind = char.ToUpperInvariant(match.Groups["kind"].Value[0]);
        Group digitGroup = match.Groups["n"];
        if (kind == 'G') return digitGroup.Success ? null : ('G', 0, false);
        bool explicitDecimals = digitGroup.Success;
        int decimals = explicitDecimals ? int.Parse(digitGroup.Value, CultureInfo.InvariantCulture) : (kind == 'K' ? 1 : 2);
        return decimals <= MaxDecimals ? (kind, decimals, explicitDecimals) : null;
    }

    /// <summary>True when <paramref name="format"/> is in the chart number grammar (null and empty mean <c>G</c>).</summary>
    public static bool IsSupported(string? format) => Parse(format) is not null;

    private static (string Digits, int Scale) RoundDecimal(string digits, int scale, int n)
    {
        if (scale > n)
        {
            int cut = scale - n;
            string padded = digits.PadLeft(cut + 1, '0');
            string kept = padded[..^cut];
            char next = padded[padded.Length - cut];
            string rounded = next >= '5'
                ? (BigInteger.Parse(kept, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture)
                : kept;
            return (rounded, n);
        }
        return (digits + new string('0', n - scale), n);
    }

    /// <summary>
    /// Formats <paramref name="value"/> with <paramref name="format"/>.
    /// </summary>
    /// <exception cref="FormatException">The format is outside the grammar.</exception>
    public static string Format(decimal value, string? format = null)
    {
        (char kind, int decimals, bool explicitDecimals) = Parse(format)
            ?? throw new FormatException(
                $"Unsupported chart number format '{format}'. Use G, F<n>, N<n>, P<n> or K<n>, with n from 0 to {MaxDecimals}.");

        string text = value.ToString(CultureInfo.InvariantCulture);
        bool negative = text.StartsWith('-');
        if (negative) text = text[1..];
        int point = text.IndexOf('.', StringComparison.Ordinal);
        string digits = point < 0 ? text : text.Remove(point, 1);
        int scale = point < 0 ? 0 : text.Length - point - 1;

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

        if (kind == 'K')
        {
            int intDigits = string.Equals(digits, "0", StringComparison.Ordinal) ? 0 : digits.Length - scale;
            int tier = intDigits <= 3 ? 0 : Math.Min(CompactSuffixes.Length - 1, (intDigits - 1) / 3);
            int n = decimals;
            if (tier == 0 && !explicitDecimals && scale == 0) n = 0;

            int shiftedScale = scale + tier * 3;
            (string rDigits, int rScale) = RoundDecimal(digits, shiftedScale, n);
            string bodyC = rDigits.PadLeft(rScale + 1, '0');
            string wholeC = rScale == 0 ? bodyC : bodyC[..^rScale];
            string fractionC = rScale == 0 ? "" : bodyC[^rScale..];

            if (wholeC.Length > 3 && tier < CompactSuffixes.Length - 1)
            {
                tier += 1;
                n = decimals;
                shiftedScale = scale + tier * 3;
                (rDigits, rScale) = RoundDecimal(digits, shiftedScale, n);
                bodyC = rDigits.PadLeft(rScale + 1, '0');
                wholeC = rScale == 0 ? bodyC : bodyC[..^rScale];
                fractionC = rScale == 0 ? "" : bodyC[^rScale..];
            }

            bool isZeroC = wholeC.Trim('0').Length == 0 && fractionC.Trim('0').Length == 0;
            var resultC = new StringBuilder();
            if (negative && !isZeroC) resultC.Append('-');
            resultC.Append(wholeC);
            if (fractionC.Length > 0) resultC.Append('.').Append(fractionC);
            resultC.Append(CompactSuffixes[tier]);
            return resultC.ToString();
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
            (digits, scale) = RoundDecimal(digits, scale, decimals);
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
