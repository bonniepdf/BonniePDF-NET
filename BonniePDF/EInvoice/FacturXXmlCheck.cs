using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BonniePDF;

/// <summary>
/// The invoice XML check behind <see cref="FacturX.Apply"/> (spec 04 §11.4).
///
/// <para>A small, single-pass well-formedness reader rather than <c>XmlReader</c>: it exists to answer
/// two questions the Factur-X claim depends on (is this CII, and which profile does it declare?) and to
/// refuse input it cannot read safely. Document type declarations are refused outright, so no entity is
/// ever expanded. The rules are the specification's, not a platform parser's, so every implementation
/// accepts and rejects the same bytes.</para>
/// </summary>
internal static partial class FacturXXmlCheck
{
    private const string CiiNamespace = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private const string RamNamespace = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private const string En16931 = "urn:cen.eu:en16931:2017";

    private static readonly Dictionary<string, FacturXProfile> Guidelines = new(StringComparer.Ordinal)
    {
        ["urn:factur-x.eu:1p0:minimum"] = FacturXProfile.Minimum,
        ["urn:factur-x.eu:1p0:basicwl"] = FacturXProfile.BasicWl,
        [En16931 + "#compliant#urn:factur-x.eu:1p0:basic"] = FacturXProfile.Basic,
        [En16931] = FacturXProfile.En16931,
        [En16931 + "#conformant#urn:factur-x.eu:1p0:extended"] = FacturXProfile.Extended,
    };

    [GeneratedRegex(@"\Aurn:cen\.eu:en16931:2017#compliant#urn:xeinkauf\.de:kosit:xrechnung_[0-9]+\.[0-9]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex XRechnung();

    [GeneratedRegex(@"\sencoding\s*=\s*(?<q>[""'])(?<value>[^""']*)\k<q>", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EncodingDeclaration();

    /// <summary>The profile a BT-24 identifier declares, or <c>null</c> when it is not one core recognises.</summary>
    internal static FacturXProfile? ProfileForGuideline(string id) =>
        Guidelines.TryGetValue(id, out var profile) ? profile : XRechnung().IsMatch(id) ? FacturXProfile.XRechnung : null;

    /// <summary>
    /// Checks <paramref name="bytes"/> as Factur-X invoice XML and returns the BT-24 identifier and the
    /// profile it declares. Throws <see cref="FacturXException"/> on the first failure, in the
    /// specification's order.
    /// </summary>
    internal static (string Guideline, FacturXProfile Profile) Check(byte[] bytes)
    {
        string text = Decode(bytes);
        var result = new Scanner(text).Run();

        if (!string.Equals(result.RootName, "CrossIndustryInvoice", StringComparison.Ordinal)
            || !string.Equals(result.RootNamespace, CiiNamespace, StringComparison.Ordinal))
        {
            string found = result.RootNamespace is null ? result.RootName : $"{{{result.RootNamespace}}}{result.RootName}";
            throw new FacturXException("not-cii", $"Factur-X invoice XML must be a CII CrossIndustryInvoice; the root is {found}");
        }

        string id = result.Guideline is null ? "" : TrimXmlWhitespace(result.Guideline);
        if (id.Length == 0)
        {
            throw new FacturXException(
                "guideline-missing",
                "Factur-X invoice XML has no profile identifier (ExchangedDocumentContext/GuidelineSpecifiedDocumentContextParameter/ID)");
        }

        var profile = ProfileForGuideline(id) ?? throw new FacturXException(
            "guideline-unrecognized",
            $"Factur-X invoice XML declares the profile identifier \"{id}\", which is not a Factur-X / ZUGFeRD 2.1+ profile");
        return (id, profile);
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes.Length >= 2 && ((bytes[0] == 0xFE && bytes[1] == 0xFF) || (bytes[0] == 0xFF && bytes[1] == 0xFE)))
            throw new FacturXException("unsupported-encoding", "Factur-X invoice XML must be UTF-8; it starts with a UTF-16 byte order mark");

        int start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes, start, bytes.Length - start);
        }
        catch (DecoderFallbackException)
        {
            throw new FacturXException("malformed-xml", "Factur-X invoice XML is not valid UTF-8");
        }

        // The declared encoding is checked before well-formedness, so a Latin-1 file is reported as
        // such even if the rest would also fail.
        if (text.StartsWith("<?xml", StringComparison.Ordinal) && text.Length > 5 && IsDeclarationEnd(text[5]))
        {
            int end = text.IndexOf("?>", StringComparison.Ordinal);
            if (end != -1)
            {
                var encoding = EncodingDeclaration().Match(text, 5, end - 5);
                if (encoding.Success && !string.Equals(encoding.Groups["value"].Value, "utf-8", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FacturXException(
                        "unsupported-encoding",
                        $"Factur-X invoice XML must be UTF-8; it declares encoding \"{encoding.Groups["value"].Value}\"");
                }
            }
        }

        return text;
    }

    private static bool IsDeclarationEnd(char c) => c is ' ' or '\t' or '\n' or '\r' or '?';

    private static bool IsXmlWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r';

    private static string TrimXmlWhitespace(string s)
    {
        int start = 0, end = s.Length;
        while (start < end && IsXmlWhitespace(s[start])) start++;
        while (end > start && IsXmlWhitespace(s[end - 1])) end--;
        return s[start..end];
    }

    /// <summary>A UTF-16 code unit that may appear literally. Surrogates are paired: the text came from strict UTF-8.</summary>
    private static bool IsCharUnit(char c) => c >= 0x20 ? c != '￾' && c != '￿' : c is '\t' or '\n' or '\r';

    private static bool IsCharCodePoint(int c) =>
        c is 0x09 or 0x0A or 0x0D
        || (c >= 0x20 && c <= 0xD7FF)
        || (c >= 0xE000 && c <= 0xFFFD)
        || (c >= 0x10000 && c <= 0x10FFFF);

    private static bool IsNameStart(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || c >= 0x80;

    private static bool IsNameChar(char c) => IsNameStart(c) || (c >= '0' && c <= '9') || c == '.' || c == '-';

    private static FacturXException Malformed(string detail, int offset) =>
        new("malformed-xml", $"Factur-X invoice XML is not well-formed: {detail} at offset {offset}");

    private static string Hex(char c) => "U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture);

    private sealed record OpenElement(string QName, Dictionary<string, string> Namespaces, int Step);

    private readonly record struct ScanResult(string RootName, string? RootNamespace, string? Guideline);

    private sealed class Scanner(string s)
    {
        private int _pos;
        private readonly List<OpenElement> _stack = [];
        private bool _rootSeen;
        private string _rootName = "";
        private string? _rootNamespace;
        private StringBuilder? _capture;
        private string? _guideline;
        private bool _guidelineFound;

        private OpenElement? Top => _stack.Count == 0 ? null : _stack[^1];

        public ScanResult Run()
        {
            while (_pos < s.Length)
            {
                if (s[_pos] == '<') Markup();
                else Text();
            }
            if (!_rootSeen) throw Malformed("no root element", _pos);
            if (_stack.Count > 0) throw Malformed($"<{_stack[^1].QName}> is not closed", _pos);
            return new ScanResult(_rootName, _rootNamespace, _guideline);
        }

        private bool At(int index, string token) => string.CompareOrdinal(s, index, token, 0, token.Length) == 0;

        private char CharAt(int index) => index < s.Length ? s[index] : '\0';

        private void Markup()
        {
            int at = _pos;
            if (At(at, "<!--"))
            {
                int end = s.IndexOf("-->", at + 4, StringComparison.Ordinal);
                if (end == -1) throw Malformed("unterminated comment", at);
                CheckChars(at + 4, end);
                _pos = end + 3;
            }
            else if (At(at, "<![CDATA["))
            {
                if (_stack.Count == 0) throw Malformed("CDATA section outside the root element", at);
                int end = s.IndexOf("]]>", at + 9, StringComparison.Ordinal);
                if (end == -1) throw Malformed("unterminated CDATA section", at);
                CheckChars(at + 9, end);
                _capture?.Append(s, at + 9, end - (at + 9));
                _pos = end + 3;
            }
            else if (At(at, "<!DOCTYPE"))
            {
                throw new FacturXException(
                    "doctype-forbidden",
                    $"Factur-X invoice XML must not contain a document type declaration (at offset {at})");
            }
            else if (At(at, "<!"))
            {
                throw Malformed("unexpected declaration", at);
            }
            else if (At(at, "<?"))
            {
                ProcessingInstruction(at);
            }
            else if (CharAt(at + 1) == '/')
            {
                EndTag(at);
            }
            else
            {
                StartTag(at);
            }
        }

        private void ProcessingInstruction(int at)
        {
            int end = s.IndexOf("?>", at + 2, StringComparison.Ordinal);
            if (end == -1) throw Malformed("unterminated processing instruction", at);
            int i = at + 2;
            while (i < end && !IsXmlWhitespace(s[i])) i++;
            string target = s[(at + 2)..i];
            if (target.Length == 0) throw Malformed("processing instruction without a target", at);
            if (string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase) && at != 0)
                throw Malformed("XML declaration not at the start", at);
            CheckChars(at + 2, end);
            _pos = end + 2;
        }

        private void EndTag(int at)
        {
            int nameEnd = ReadName(at + 2);
            string qname = s[(at + 2)..nameEnd];
            int close = SkipWhitespace(nameEnd);
            if (CharAt(close) != '>') throw Malformed("malformed end tag", at);
            var open = Top ?? throw Malformed($"end tag </{qname}> without a start tag", at);
            _stack.RemoveAt(_stack.Count - 1);
            if (!string.Equals(open.QName, qname, StringComparison.Ordinal)) throw Malformed($"end tag </{qname}> does not close <{open.QName}>", at);
            Closed(open);
            _pos = close + 1;
        }

        private void StartTag(int at)
        {
            if (_rootSeen && _stack.Count == 0) throw Malformed("content after the root element", at);
            int i = ReadName(at + 1);
            string qname = s[(at + 1)..i];
            var attributes = new List<KeyValuePair<string, string>>();
            var attributeNames = new HashSet<string>(StringComparer.Ordinal);
            bool selfClosing = false;

            while (true)
            {
                int afterSpace = SkipWhitespace(i);
                char c = CharAt(afterSpace);
                if (afterSpace < s.Length && c == '>')
                {
                    i = afterSpace + 1;
                    break;
                }
                if (afterSpace < s.Length && c == '/' && CharAt(afterSpace + 1) == '>')
                {
                    selfClosing = true;
                    i = afterSpace + 2;
                    break;
                }
                if (afterSpace >= s.Length) throw Malformed($"unterminated start tag <{qname}>", at);
                if (afterSpace == i) throw Malformed($"attributes of <{qname}> must be separated by whitespace", afterSpace);
                int nameEnd = ReadName(afterSpace);
                string name = s[afterSpace..nameEnd];
                int j = SkipWhitespace(nameEnd);
                if (CharAt(j) != '=' || j >= s.Length) throw Malformed($"attribute {name} has no value", afterSpace);
                j = SkipWhitespace(j + 1);
                char quote = CharAt(j);
                if (j >= s.Length || (quote != '"' && quote != '\'')) throw Malformed($"attribute {name} value is not quoted", j);
                int valueEnd = s.IndexOf(quote, j + 1);
                if (valueEnd == -1) throw Malformed($"unterminated value for attribute {name}", j);
                int lt = s.IndexOf('<', j + 1);
                if (lt != -1 && lt < valueEnd) throw Malformed($"'<' in attribute {name}", lt);
                if (!attributeNames.Add(name)) throw Malformed($"duplicate attribute {name}", afterSpace);
                attributes.Add(new(name, Resolve(j + 1, valueEnd)));
                i = valueEnd + 1;
            }

            var parent = Top;
            var namespaces = parent is null
                ? new Dictionary<string, string>(StringComparer.Ordinal) { ["xml"] = XmlNamespace }
                : new Dictionary<string, string>(parent.Namespaces, StringComparer.Ordinal);
            foreach (var (name, value) in attributes)
            {
                if (string.Equals(name, "xmlns", StringComparison.Ordinal))
                {
                    namespaces[""] = value;
                }
                else if (name.StartsWith("xmlns:", StringComparison.Ordinal))
                {
                    if (value.Length == 0) throw Malformed($"empty namespace declaration {name}", at);
                    namespaces[name[6..]] = value;
                }
            }
            foreach (var (name, _) in attributes)
            {
                int attributeColon = name.IndexOf(':');
                if (attributeColon == -1) continue;
                string attributePrefix = name[..attributeColon];
                if (!string.Equals(attributePrefix, "xmlns", StringComparison.Ordinal) && !namespaces.ContainsKey(attributePrefix))
                    throw Malformed($"undeclared prefix {attributePrefix}:", at);
            }

            int colon = qname.IndexOf(':');
            string prefix = colon == -1 ? "" : qname[..colon];
            string local = colon == -1 ? qname : qname[(colon + 1)..];
            if (prefix.Length > 0 && !namespaces.ContainsKey(prefix)) throw Malformed($"undeclared prefix {prefix}:", at);
            string? ns = namespaces.TryGetValue(prefix, out var uri) && uri.Length > 0 ? uri : null;

            int step = 0;
            if (parent is null)
            {
                _rootSeen = true;
                _rootName = local;
                _rootNamespace = ns;
                step = 1;
            }
            else if (parent.Step == 1 && Is(local, "ExchangedDocumentContext") && Is(ns, CiiNamespace)) step = 2;
            else if (parent.Step == 2 && Is(local, "GuidelineSpecifiedDocumentContextParameter") && Is(ns, RamNamespace)) step = 3;
            else if (parent.Step == 3 && Is(local, "ID") && Is(ns, RamNamespace) && !_guidelineFound) step = 4;

            var element = new OpenElement(qname, namespaces, step);
            if (step == 4)
            {
                _guidelineFound = true;
                _capture = new StringBuilder();
            }
            if (selfClosing) Closed(element);
            else _stack.Add(element);
            _pos = i;
        }

        private static bool Is(string? a, string b) => string.Equals(a, b, StringComparison.Ordinal);

        private void Closed(OpenElement element)
        {
            if (element.Step != 4) return;
            _guideline = _capture?.ToString();
            _capture = null;
        }

        private void Text()
        {
            int at = _pos;
            int end = s.IndexOf('<', at);
            if (end == -1) end = s.Length;
            if (_stack.Count == 0)
            {
                for (int i = at; i < end; i++)
                {
                    if (!IsXmlWhitespace(s[i])) throw Malformed("text outside the root element", i);
                }
            }
            else
            {
                string value = Resolve(at, end);
                if (_capture is not null && Top!.Step == 4) _capture.Append(value);
            }
            _pos = end;
        }

        /// <summary>Validates characters and references in <c>[start, end)</c> and returns the text with references resolved.</summary>
        private string Resolve(int start, int end)
        {
            var output = new StringBuilder(end - start);
            int i = start;
            while (i < end)
            {
                char c = s[i];
                if (c != '&')
                {
                    if (!IsCharUnit(c)) throw Malformed($"character {Hex(c)}", i);
                    output.Append(c);
                    i++;
                    continue;
                }
                int semi = s.IndexOf(';', i + 1);
                if (semi == -1 || semi >= end) throw Malformed("'&' that does not begin a reference", i);
                string body = s[(i + 1)..semi];
                if (body.StartsWith('#'))
                {
                    bool hex = body.StartsWith("#x", StringComparison.Ordinal);
                    string digits = body[(hex ? 2 : 1)..];
                    if (digits.Length == 0 || digits.Length > 8 || !AllDigits(digits, hex))
                        throw Malformed($"character reference &{body};", i);
                    int code = int.Parse(digits, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture);
                    if (!IsCharCodePoint(code)) throw Malformed($"character reference &{body};", i);
                    output.Append(char.ConvertFromUtf32(code));
                }
                else
                {
                    output.Append(body switch
                    {
                        "amp" => "&",
                        "lt" => "<",
                        "gt" => ">",
                        "quot" => "\"",
                        "apos" => "'",
                        _ => throw Malformed($"unknown entity &{body};", i),
                    });
                }
                i = semi + 1;
            }
            return output.ToString();
        }

        private static bool AllDigits(string digits, bool hex)
        {
            foreach (char d in digits)
            {
                bool ok = (d >= '0' && d <= '9') || (hex && ((d >= 'a' && d <= 'f') || (d >= 'A' && d <= 'F')));
                if (!ok) return false;
            }
            return true;
        }

        private void CheckChars(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (!IsCharUnit(s[i])) throw Malformed($"character {Hex(s[i])}", i);
            }
        }

        /// <summary>Reads a QName starting at <paramref name="start"/> and returns the index after it.</summary>
        private int ReadName(int start)
        {
            int i = start;
            while (i < s.Length && (IsNameChar(s[i]) || s[i] == ':')) i++;
            string name = s[start..i];
            int colon = name.IndexOf(':');
            if (name.Length == 0
                || !IsNameStart(name[0])
                || (colon != -1 && (colon == name.Length - 1 || name.IndexOf(':', colon + 1) != -1)))
            {
                throw Malformed(name.Length == 0 ? "missing name" : $"invalid name {name}", start);
            }
            return i;
        }

        private int SkipWhitespace(int i)
        {
            while (i < s.Length && IsXmlWhitespace(s[i])) i++;
            return i;
        }
    }
}
