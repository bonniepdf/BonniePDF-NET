using System;

namespace BonniePDF;

/// <summary>Culture-invariant formatting for numbers printed in documents.</summary>
public static class NumberFormatter
{
    /// <summary>Formats a number with the portable G, F, N, or P grammar, with optional prefix and positive sign.</summary>
    public static string Format(decimal value, string? format = null, string prefix = "", bool showPositiveSign = false)
    {
        string text = ChartNumberFormat.Format(value, format);
        if (text.StartsWith('-')) return "-" + prefix + text[1..];
        string sign = showPositiveSign && text.AsSpan().IndexOfAnyInRange('1', '9') >= 0 ? "+" : "";
        return sign + prefix + text;
    }
}
