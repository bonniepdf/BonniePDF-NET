using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace BonniePDF.Tests.Cross_Language;

/// <summary>
/// Content streams compared across implementations: operators and every non-numeric
/// token exactly, numeric operands within one unit in the third decimal place. .NET lays
/// out in <c>float</c> and TypeScript in <c>number</c>, so a printed coordinate can differ
/// in its last digit; anything larger is a parity bug. The cases mirror the shared
/// numeric-tolerance conformance fixture and
/// core/tests/cross-language/content-stream-parity.test.ts, expected strings included.
/// </summary>
public class ContentStreamParityTests
{
    private static readonly Encoding Latin1 = Encoding.Latin1;

    [Theory]
    [InlineData("identical", "q\n1 0 0 rg\n10 980 m\nQ\n", "q\n1 0 0 rg\n10 980 m\nQ\n", true)]
    [InlineData("last-digit", "567.243 20 m\n", "567.242 20 m\n", true)]
    [InlineData("colour-operand", "0.258 0.1 0.9 rg\n", "0.257 0.1 0.9 rg\n", true)]
    [InlineData("integer-against-decimal", "10 980 m\n", "10.001 980 m\n", true)]
    [InlineData("negative-zero", "0 -0.001 m\n", "0 0 m\n", true)]
    [InlineData("two-units", "567.244 20 m\n", "567.242 20 m\n", false)]
    [InlineData("operator-differs", "1 0 0 1 10 20 cm\n", "1 0 0 1 10 20 Tm\n", false)]
    [InlineData("operand-count-differs", "10 20 m\n", "10 20 30 m\n", false)]
    [InlineData("name-differs", "/F1 12 Tf\n", "/F2 12 Tf\n", false)]
    [InlineData("string-exact", "(Total 10.5) Tj\n", "(Total 10.4) Tj\n", false)]
    [InlineData("string-with-escaped-paren", "(a\\) 1.001) Tj\n", "(a\\) 1.001) Tj\n", true)]
    [InlineData("hex-string-exact", "<0041> Tj\n", "<0042> Tj\n", false)]
    [InlineData("array-numbers", "[(A) -120.501 (B)] TJ\n", "[(A) -120.5 (B)] TJ\n", true)]
    [InlineData("dictionary", "/Span <</MCID 3>> BDC\n", "/Span <</MCID 3>> BDC\n", true)]
    [InlineData("whitespace-layout", "10 20 m 30 40 l\n", "10 20 m\n30 40 l\n", true)]
    public void NumericTolerance(string id, string a, string b, bool equal)
    {
        Assert.True(Matches(a, b) == equal, $"{id}: expected {(equal ? "a match" : "a mismatch")}");
        Assert.True(Matches(b, a) == equal, $"{id} (reversed): expected {(equal ? "a match" : "a mismatch")}");
    }

    /// <summary>
    /// Columns weighted 1.4 and 1: 1.4 is not exact in single precision, so .NET and
    /// TypeScript start from different numbers. The expected stream is TypeScript's output
    /// for the same document.
    /// </summary>
    [Fact]
    public void WeightedColumns_MatchTypeScriptWithinTolerance()
    {
        const string typeScript =
            "q\nBT\n0 0 0 rg\n1 0 0 1 50 781.19 Tm\n/F1 10.7 Tf\n(Left column, weighted one point four, long enough to wrap) Tj\nET\n" +
            "BT\n0 0 0 rg\n1 0 0 1 50 768.35 Tm\n/F1 10.7 Tf\n(across several lines of text.) Tj\nET\n" +
            "BT\n0 0 0 rg\n1 0 0 1 341.952 781.19 Tm\n/F1 10.7 Tf\n(Right column, weighted one.) Tj\nET\nQ\n";

        var doc = new BonniePDF.Document();
        var row = new HorizontalStack { WidthDistribution = WidthDistribution.Weighted, Spacing = 7.3f };
        row.Add(new TextBlock("Left column, weighted one point four, long enough to wrap across several lines of text.") { FontSize = 10.7f }, 1.4f);
        row.Add(new TextBlock("Right column, weighted one.") { FontSize = 10.7f }, 1f);
        doc.Add(row);

        string dotnet = FirstContentStream(doc.GenerateBytes());

        Assert.True(Matches(dotnet, typeScript), $"Content streams differ beyond tolerance.\n.NET:\n{dotnet}\nTypeScript:\n{typeScript}");
    }

    /// <summary>True when two content streams match under the shared tolerance rule.</summary>
    internal static bool Matches(string a, string b)
    {
        List<string> left = Tokenize(a);
        List<string> right = Tokenize(b);
        if (left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
        {
            if (string.Equals(left[i], right[i], StringComparison.Ordinal)) continue;
            if (!TryThousandths(left[i], out long x) || !TryThousandths(right[i], out long y)) return false;
            if (Math.Abs(x - y) > 1) return false;
        }
        return true;
    }

    private static bool TryThousandths(string token, out long value)
    {
        value = 0;
        if (token.Length == 0) return false;
        int start = token[0] is '+' or '-' ? 1 : 0;
        bool digit = false;
        bool point = false;
        for (int i = start; i < token.Length; i++)
        {
            if (char.IsAsciiDigit(token[i])) digit = true;
            else if (token[i] == '.' && !point) point = true;
            else return false;
        }
        if (!digit) return false;
        value = (long)Math.Round(double.Parse(token, CultureInfo.InvariantCulture) * 1000, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool IsWhitespace(char c) => c is ' ' or '\n' or '\r' or '\t' or '\f' or '\0';

    private static bool IsDelimiter(char c) => c is '(' or ')' or '<' or '>' or '[' or ']' or '{' or '}' or '/' or '%';

    private static List<string> Tokenize(string s)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (IsWhitespace(c)) { i++; continue; }
            int start = i;
            if (c == '(')
            {
                int depth = 0;
                for (; i < s.Length; i++)
                {
                    if (s[i] == '\\') { i++; continue; }
                    if (s[i] == '(') depth++;
                    else if (s[i] == ')' && --depth == 0) { i++; break; }
                }
            }
            else if (c == '<' && i + 1 < s.Length && s[i + 1] == '<') i += 2;
            else if (c == '>' && i + 1 < s.Length && s[i + 1] == '>') i += 2;
            else if (c == '<')
            {
                int close = s.IndexOf('>', i);
                i = close < 0 ? s.Length : close + 1;
            }
            else if (c is '[' or ']' or '{' or '}') i++;
            else
            {
                i++;
                while (i < s.Length && !IsWhitespace(s[i]) && !IsDelimiter(s[i])) i++;
            }
            tokens.Add(s.Substring(start, i - start));
        }
        return tokens;
    }

    private static string FirstContentStream(byte[] bytes)
    {
        string text = Latin1.GetString(bytes);
        int contentStart = text.IndexOf("stream", StringComparison.Ordinal) + "stream".Length;
        if (text[contentStart] == '\r') contentStart++;
        if (text[contentStart] == '\n') contentStart++;
        int end = text.IndexOf("endstream", contentStart, StringComparison.Ordinal);
        byte[] raw = Latin1.GetBytes(text.Substring(contentStart, end - contentStart));
        try
        {
            using var input = new MemoryStream(raw);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(zlib, Latin1);
            return reader.ReadToEnd();
        }
        catch (InvalidDataException)
        {
            return Latin1.GetString(raw);
        }
    }
}
