using System;
using System.Globalization;
using System.Text;
using BonniePDF.Models;
using BonniePDF.Typography;

namespace BonniePDF.IO;

/// <summary>
/// Shared utilities for PDF syntax encoding and formatting.
/// </summary>
internal static class PdfSyntax
{
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

        return sb.ToString();
    }

    /// <summary>
    /// Formats a DateTime as a PDF date string (D:YYYYMMDDHHmmSSOHH'mm').
    /// </summary>
    internal static string FormatDate(DateTime date)
    {
        // PDF date format: D:YYYYMMDDHHmmSSOHH'mm'
        // For simplicity, we'll treat all dates as local time or UTC based on Kind
        string offset = date.Kind == DateTimeKind.Utc ? "Z" : "";
        return $"D:{date:yyyyMMddHHmmss}{offset}";
    }

    /// <summary>
    /// Formats a float using InvariantCulture to ensure dot separators.
    /// This is critical for PDF syntax which requires decimal points, not commas.
    /// </summary>
    internal static string FormatDecimal(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a color as an RGB triplet (e.g., "0.5 0.5 0.5").
    /// Uses InvariantCulture to ensure consistent decimal separators across all locales.
    /// </summary>
    internal static string FormatColor(Color c)
    {
        return $"{FormatDecimal(c.ScR)} {FormatDecimal(c.ScG)} {FormatDecimal(c.ScB)}";
    }

    /// <summary>
    /// Encodes a string as a PDF hex string (surrounded by angle brackets).
    /// Example: "AB" -&gt; "&lt;00410042&gt;"
    /// Used for embedded font glyph IDs and high-ASCII strings.
    /// </summary>
    internal static string ToHexString(string text)
    {
        if (string.IsNullOrEmpty(text)) return "<>";

        StringBuilder sb = new(text.Length * 4 + 2);
        sb.Append('<');
        foreach (char c in text)
            // Format as 4-digit hex for Unicode (UTF-16BE effectively)
            sb.Append(((int)c).ToString("X4"));
        sb.Append('>');
        return sb.ToString();
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

        foreach (char c in text)
        {
            // Get the specific Glyph ID from the TrueType font table
            ushort gid = font.GetGlyphId(c);

            // Format as 4-digit uppercase hex (Big Endian)
            sb.Append(gid.ToString("X4"));
        }

        sb.Append('>');
        return sb.ToString();
    }
}