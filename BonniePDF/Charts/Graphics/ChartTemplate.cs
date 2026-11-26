// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace BonniePDF;

/// <summary>
/// Fills a text template. <c>{name}</c> inserts a token; a numeric token takes an optional format, <c>{name:P0}</c>, from the chart
/// number formats (<c>G</c> when omitted). A token the template names but the caller did not supply throws.
/// </summary>
internal static class ChartTemplate
{
    private static bool IsWord(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';

    internal static string Apply(string template, IReadOnlyDictionary<string, object> tokens)
    {
        StringBuilder result = new();
        int i = 0;
        while (i < template.Length)
        {
            if (template[i] == '{' && TryParseToken(template, i, out string name, out string? format, out int end))
            {
                if (!tokens.TryGetValue(name, out object? value)) throw new InvalidOperationException($"Unknown template token '{name}'.");
                result.Append(value switch
                {
                    string s => s,
                    double d => ChartNumberFormat.Format(ToDecimal(d), format ?? "G"),
                    decimal d => ChartNumberFormat.Format(d, format ?? "G"),
                    _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
                });
                i = end;
            }
            else result.Append(template[i++]);
        }
        return result.ToString();
    }

    /// <summary>Reads <c>{name}</c> or <c>{name:format}</c> at <paramref name="start"/>.</summary>
    private static bool TryParseToken(string text, int start, out string name, out string? format, out int end)
    {
        name = "";
        format = null;
        end = start;
        int i = start + 1;
        int nameStart = i;
        while (i < text.Length && IsWord(text[i])) i++;
        if (i == nameStart || i >= text.Length) return false;
        name = text.Substring(nameStart, i - nameStart);
        if (text[i] == ':')
        {
            int formatStart = ++i;
            while (i < text.Length && text[i] != '}') i++;
            if (i == formatStart || i >= text.Length) return false;
            format = text.Substring(formatStart, i - formatStart);
        }
        if (text[i] != '}') return false;
        end = i + 1;
        return true;
    }

    /// <summary>The decimal with the shortest round-trip digits of <paramref name="value"/>, as TypeScript reads a number.</summary>
    internal static decimal ToDecimal(double value) =>
        decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
}
