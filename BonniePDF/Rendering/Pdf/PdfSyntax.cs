using System;
using System.Globalization;
using System.Text;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Rendering.Pdf;

/// <summary>
/// Shared utilities for PDF syntax encoding and formatting.
/// </summary>
internal static class PdfSyntax
{
    /// <summary>
    /// Encodes a string for use in a PDF document information dictionary.
    /// Returns a PDF literal string for Latin-1 compatible text, or a UTF-16 BE hex
    /// string (with BOM) for text that contains characters outside the Latin-1 range.
    /// </summary>
    internal static string EncodeMetadataString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "()";
        StringBuilder result = new(input.Length + 10);
        AppendMetadataString(result, input);
        return result.ToString();
    }

    internal static void AppendMetadataString(StringBuilder result, string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            result.Append("()");
            return;
        }

        foreach (char c in input)
        {
            if (c > '\xFF')
            {
                // Encode as UTF-16 BE hex string with BOM (0xFEFF)
                result.Append("<FEFF");
                for (int i = 0; i < input.Length; i++)
                {
                    char value = input[i];
                    if (char.IsHighSurrogate(value) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                    {
                        AppendHex4(result, value);
                        AppendHex4(result, input[++i]);
                    }
                    else AppendHex4(result, char.IsSurrogate(value) ? (ushort)0xfffd : value);
                }
                result.Append('>');
                return;
            }
        }

        result.Append('(');
        AppendEscaped(result, input);
        result.Append(')');
    }

    internal static void AppendHex4(StringBuilder result, ushort value)
    {
        const string digits = "0123456789ABCDEF";
        result.Append(digits[value >> 12]).Append(digits[(value >> 8) & 15])
            .Append(digits[(value >> 4) & 15]).Append(digits[value & 15]);
    }

    /// <summary>
    /// Escapes a string for use within PDF string literals (parentheses).
    /// Handles backslashes, parentheses, and control characters.
    /// </summary>
    internal static string Escape(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Pre-size the builder to avoid internal re-allocations
        // (Assuming average 10% expansion for escaped chars)
        StringBuilder sb = new(input.Length + 10);

        AppendEscaped(sb, input);
        return sb.ToString();
    }

    internal static void AppendEscaped(StringBuilder sb, string input)
    {
        foreach (char c in input)
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '(': sb.Append(@"\("); break;
                case ')': sb.Append(@"\)"); break;
                case '\r': sb.Append(@"\r"); break;
                case '\n': sb.Append(@"\n"); break;
                case '\t': sb.Append(@"\t"); break;
                case '\b': sb.Append(@"\b"); break;
                case '\f': sb.Append(@"\f"); break;
                default: sb.Append(c); break;
            }

    }

    /// <summary>
    /// Formats a DateTimeOffset as a PDF date string (D:YYYYMMDDHHmmSSOHH'mm').
    /// </summary>
    internal static string FormatDate(DateTimeOffset date)
    {
        // PDF date format: D:YYYYMMDDHHmmSSOHH'mm'
        // where O is '+', '-', or 'Z', and HH'mm' is the UTC offset
        string datePart = date.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        TimeSpan offset = date.Offset;
        if (offset == TimeSpan.Zero)
            return $"D:{datePart}Z";
        char sign = offset > TimeSpan.Zero ? '+' : '-';
        TimeSpan abs = offset.Duration();
        return $"D:{datePart}{sign}{abs.Hours:D2}'{abs.Minutes:D2}'";
    }

    /// <summary>
    /// Formats a float using InvariantCulture to ensure dot separators.
    /// This is critical for PDF syntax which requires decimal points, not commas.
    /// </summary>
    /// <summary>Appends <see cref="FormatDecimal(float)"/>'s text without allocating a string.</summary>
    internal static StringBuilder AppendDecimal(StringBuilder s, float value)
    {
        Span<char> buffer = stackalloc char[64];
        if (!value.TryFormat(buffer, out int written, "0.###", CultureInfo.InvariantCulture))
            return s.Append(FormatDecimal(value));
        ReadOnlySpan<char> text = buffer[..written];
        return text is "-0" ? s.Append('0') : s.Append(text);
    }

    internal static string FormatDecimal(float value)
    {
        // A negative zero - a zero matrix entry negated, or a tiny negative value
        // rounded away - prints as "0", as it does in the TypeScript implementation
        // and in the double overload below.
        string s = value.ToString("0.###", CultureInfo.InvariantCulture);
        return s == "-0" ? "0" : s;
    }

    /// <summary>
    /// Formats a double with the same "0.###" shape, without narrowing to float first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Geometry that is computed in double must be formatted from the double. A
    /// float round-trip on the way to the formatter moves a value by up to ~1e-4 at
    /// page scale, which is enough to carry it across a 0.0005 rounding boundary and
    /// print a different last digit than the TypeScript implementation does for the
    /// same input. Narrow at the sink boundary, not before the formatter.
    /// </para>
    /// <para>
    /// The rounding has to match too. A custom format string such as "0.###"
    /// first rounds a double to 15 significant digits, so 779.6924999999999
    /// becomes 779.6925 and then prints 779.693. The standard "F3" format rounds
    /// the exact binary value, as TypeScript's <c>toFixed(3)</c> does, and prints
    /// 779.692 - except on an exact tie, which "F3" rounds to even and
    /// <c>toFixed</c> rounds away from zero. A double can only sit exactly
    /// halfway between two thousandths when sixteen times it is an odd integer
    /// (a tie is an odd number of two-thousandths, and only the ones that reduce
    /// to sixteenths are representable), so that case is rounded away from zero
    /// explicitly. Trailing zeros, a bare point and a negative zero are then
    /// trimmed exactly as the TypeScript formatter trims them.
    /// </para>
    /// </remarks>
    internal static string FormatDecimal(double value)
    {
        double sixteenths = value * 16; // exact: a power-of-two scale
        if (Math.Floor(sixteenths) == sixteenths && Math.Abs(sixteenths % 2) == 1)
            value = Math.CopySign(Math.Floor(Math.Abs(value) * 1000 + 0.5), value) / 1000;

        string s = value.ToString("F3", CultureInfo.InvariantCulture);
        if (s.Contains('.', StringComparison.Ordinal)) s = s.TrimEnd('0').TrimEnd('.');
        return s == "-0" ? "0" : s;
    }

    /// <summary>
    /// Formats a color as an RGB triplet (e.g., "0.5 0.5 0.5").
    /// Uses InvariantCulture to ensure consistent decimal separators across all locales.
    /// </summary>
    internal static string FormatColor(Color c)
    {
        return $"{FormatDecimal(c.ScR)} {FormatDecimal(c.ScG)} {FormatDecimal(c.ScB)}";
    }

    /// <summary>Appends <see cref="FormatColor"/>'s triplet without building it as a string.</summary>
    internal static StringBuilder AppendColor(StringBuilder s, Color c)
    {
        AppendDecimal(s, c.ScR).Append(' ');
        AppendDecimal(s, c.ScG).Append(' ');
        return AppendDecimal(s, c.ScB);
    }

    /// <summary>
    /// Converts a string into a PDF hex string of Glyph IDs (GIDs) for a specific font.
    /// Example: "£" -&gt; "&lt;00A3&gt;"
    ///     (actual hex depends on the font's internal mapping)
    ///     This bypasses character encoding issues by telling the PDF viewer exactly which shape (glyph) to draw from the
    ///     embedded font file.
    /// </summary>
    /// <param name="text">The text to convert</param>
    /// <param name="font">The TrueType font to get glyph IDs from</param>
    /// <returns>A PDF hex string containing the Glyph IDs</returns>
    internal static string ToGlyphIdHexString(string text, TtfFont font)
    {
        if (string.IsNullOrEmpty(text)) return "<>";

        // Each GID is 2 bytes, so 4 hex characters
        StringBuilder sb = new(text.Length * 4 + 2);
        sb.Append('<');

        // Use EnumerateRunes to correctly handle surrogate pairs (emoji, CJK Extension B+)
        foreach (Rune rune in text.EnumerateRunes())
        {
            // Skip control characters (newlines, tabs, etc.) - these are invisible
            // in standard fonts and should not be encoded as glyphs
            if (Rune.IsControl(rune))
                continue;

            // Get the specific Glyph ID from the TrueType font table
            ushort gid = font.GetGlyphId(rune.Value);

            // Format as 4-digit uppercase hex (Big Endian)
            sb.Append(gid.ToString("X4"));
        }

        sb.Append('>');
        return sb.ToString();
    }

    /// <summary>
    /// Converts a string into a <c>TJ</c> array operand of glyph-ID hex runs with
    /// kern-pair advance adjustments between adjacent glyphs, e.g.
    /// <c>[&lt;0024&gt; 80 &lt;0057&gt;]</c>. Adjustments are in thousandths of an em
    /// and are <em>subtracted</em> from the position by <c>TJ</c>, so a tightening
    /// kern of -80 font units becomes a positive <c>80</c> array element.
    /// </summary>
    /// <returns>
    /// The array operand, or <c>null</c> when no adjacent pair produces a non-zero
    /// adjustment; callers then keep the plain <c>Tj</c> path so unkerned strings
    /// stay byte-identical to the previous output.
    /// </returns>
    internal static string? ToKernedGlyphArray(string text, TtfFont font)
    {
        if (string.IsNullOrEmpty(text)) return null;

        StringBuilder sb = new(text.Length * 4 + 8);
        sb.Append("[<");
        bool anyKern = false;
        ushort prevGid = 0;
        bool hasPrev = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (Rune.IsControl(rune))
                continue;

            ushort gid = font.GetGlyphId(rune.Value);

            if (hasPrev)
            {
                int kern = font.GetKerning(prevGid, gid);
                // TJ subtracts its numbers from the displacement, so negate; scale
                // font units to thousandths of an em. Math.Floor(x + 0.5) matches
                // the TypeScript port's Math.round (half toward +infinity) - C#'s
                // default Math.Round is banker's rounding and would diverge.
                int adjustment = (int)Math.Floor(-kern * 1000.0 / font.UnitsPerEm + 0.5);
                if (adjustment != 0)
                {
                    sb.Append("> ").Append(adjustment).Append(" <");
                    anyKern = true;
                }
            }

            sb.Append(gid.ToString("X4"));
            prevGid = gid;
            hasPrev = true;
        }

        if (!anyKern) return null;
        sb.Append(">]");
        return sb.ToString();
    }

    /// <summary>
    /// Converts a string into a <c>TJ</c> array operand that carries both
    /// kern-pair adjustments <em>and</em> word spacing, for composite (Type0)
    /// fonts where the PDF <c>Tw</c> operator does not apply (it only affects
    /// single-byte code 32). After every space glyph an extra adjustment of
    /// <c>-(wordSpacingPts / fontSize) * 1000</c> is inserted (negative → the
    /// following glyph moves further right by <c>wordSpacingPts</c>). Letter
    /// spacing is handled separately by <c>Tc</c>. Always returns a
    /// <c>[...]</c> array; callers use it only when <paramref name="wordSpacingPts"/>
    /// is non-zero. Mirrors the TypeScript <c>toSpacedGlyphArray</c>.
    /// </summary>
    internal static string ToSpacedGlyphArray(string text, TtfFont font, float fontSize, float wordSpacingPts)
    {
        int wordAdjustment = fontSize > 0
            ? (int)Math.Floor(-wordSpacingPts * 1000.0 / fontSize + 0.5)
            : 0;

        StringBuilder sb = new(text.Length * 4 + 8);
        sb.Append("[<");
        ushort prevGid = 0;
        bool prevWasSpace = false;
        bool hasPrev = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (Rune.IsControl(rune))
                continue;

            ushort gid = font.GetGlyphId(rune.Value);

            if (hasPrev)
            {
                int kern = font.GetKerning(prevGid, gid);
                int adjustment = (int)Math.Floor(-kern * 1000.0 / font.UnitsPerEm + 0.5);
                if (prevWasSpace) adjustment += wordAdjustment;
                if (adjustment != 0)
                    sb.Append("> ").Append(adjustment).Append(" <");
            }

            sb.Append(gid.ToString("X4"));
            prevGid = gid;
            prevWasSpace = rune.Value == ' ';
            hasPrev = true;
        }

        sb.Append(">]");
        return sb.ToString();
    }
}
