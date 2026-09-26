// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BonniePDF.Rendering.Svg.Xml;

/// <summary>The kind of an <see cref="XmlEvent"/>.</summary>
internal enum XmlEventKind
{
    /// <summary>A start tag.</summary>
    Start,

    /// <summary>An end tag.</summary>
    End,

    /// <summary>Character data.</summary>
    Text
}

/// <summary>One event from the reader.</summary>
/// <param name="Kind">Which kind of event.</param>
/// <param name="Name">The element's local name, for start and end events.</param>
/// <param name="Attributes">The attributes, for start events.</param>
/// <param name="SelfClosing">True when a start tag closed itself.</param>
/// <param name="Text">The character data, for text events.</param>
internal readonly record struct XmlEvent(
    XmlEventKind Kind,
    string Name = "",
    IReadOnlyDictionary<string, string>? Attributes = null,
    bool SelfClosing = false,
    string Text = "");

/// <summary>Thrown when the document is not well formed, or is refused.</summary>
internal sealed class SvgXmlException : Exception
{
    /// <summary>Creates the exception.</summary>
    public SvgXmlException(string message, int offset) : base($"{message} (at offset {offset})") => Offset = offset;

    /// <summary>Gets the offset the failure was detected at.</summary>
    public int Offset { get; }
}

/// <summary>
/// A minimal XML reader for the SVG subset.
/// </summary>
/// <remarks>
/// <para>
/// Hand-rolled rather than using <c>XmlReader</c> so that both implementations
/// behave identically on the malformed input real exports contain, and so the
/// security posture is explicit in one place rather than spread across reader
/// settings.
/// </para>
/// <para>
/// DOCTYPE and ENTITY declarations are refused outright rather than processed.
/// External entity expansion is the classic XXE vector and an SVG importer is
/// exactly what gets pointed at untrusted input. Only the five predefined
/// entities and numeric character references are resolved.
/// </para>
/// </remarks>
internal static partial class SvgXmlReader
{
    private static readonly Dictionary<string, string> Predefined = new(StringComparer.Ordinal)
    {
        ["lt"] = "<", ["gt"] = ">", ["amp"] = "&", ["quot"] = "\"", ["apos"] = "'"
    };

    [GeneratedRegex(@"&(?<body>#x?[0-9a-fA-F]+|[a-zA-Z]+);", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EntityPattern();

    [GeneratedRegex(@"(?<name>[^ \t\n\r\f=/>]+)[ \t\n\r\f]*=[ \t\n\r\f]*(""(?<dq>[^""]*)""|'(?<sq>[^']*)')", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex AttributePattern();

    [GeneratedRegex(@"^(?<name>[^ \t\n\r\f/>]+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ElementName();

    [GeneratedRegex(@"^[/ \t\n\r\f]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex OnlySlashesAndSpace();

    /// <summary>Resolves the predefined entities and numeric character references.</summary>
    public static string ResolveEntities(string text) => EntityPattern().Replace(text, match =>
    {
        string body = match.Groups["body"].Value;

        if (body.StartsWith("#x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(body[2..], System.Globalization.NumberStyles.HexNumber, null, out int hex)
                ? SafeFromCodePoint(hex, match.Value)
                : match.Value;
        }

        if (body.StartsWith('#'))
        {
            return int.TryParse(body[1..], out int code) ? SafeFromCodePoint(code, match.Value) : match.Value;
        }

        // An unknown named entity is left as written rather than dropped: it may
        // be literal text, and deleting content silently is worse.
        return Predefined.TryGetValue(body, out string? replacement) ? replacement : match.Value;
    });

    private static string SafeFromCodePoint(int code, string fallback)
    {
        // Surrogates and out-of-range values would throw; a malformed reference
        // should not take down the parse.
        if (code < 0 || code > 0x10FFFF || code is >= 0xD800 and <= 0xDFFF) return fallback;
        return char.ConvertFromUtf32(code);
    }

    /// <summary>Reads the document as a flat event stream. No DOM is built.</summary>
    public static IEnumerable<XmlEvent> Read(string source)
    {
        int index = 0;

        while (index < source.Length)
        {
            int open = source.IndexOf('<', index);

            if (open == -1)
            {
                string trailing = source[index..];
                if (trailing.Trim().Length > 0)
                {
                    yield return new XmlEvent(XmlEventKind.Text, Text: ResolveEntities(trailing));
                }

                yield break;
            }

            if (open > index)
            {
                string text = source[index..open];
                if (text.Trim().Length > 0) yield return new XmlEvent(XmlEventKind.Text, Text: ResolveEntities(text));
            }

            if (Matches(source, open, "<!--"))
            {
                int close = source.IndexOf("-->", open + 4, StringComparison.Ordinal);
                if (close == -1) throw new SvgXmlException("unterminated comment", open);
                index = close + 3;
                continue;
            }

            if (Matches(source, open, "<![CDATA["))
            {
                int close = source.IndexOf("]]>", open + 9, StringComparison.Ordinal);
                if (close == -1) throw new SvgXmlException("unterminated CDATA section", open);
                string text = source[(open + 9)..close];
                // CDATA is literal: entities inside it are not references.
                if (text.Trim().Length > 0) yield return new XmlEvent(XmlEventKind.Text, Text: text);
                index = close + 3;
                continue;
            }

            if (Matches(source, open, "<?"))
            {
                int close = source.IndexOf("?>", open + 2, StringComparison.Ordinal);
                if (close == -1) throw new SvgXmlException("unterminated processing instruction", open);
                index = close + 2;
                continue;
            }

            if (Matches(source, open, "<!"))
            {
                string declaration = source[(open + 2)..Math.Min(source.Length, open + 9)].ToUpperInvariant();
                if (declaration.StartsWith("DOCTYPE", StringComparison.Ordinal)
                    || declaration.StartsWith("ENTITY", StringComparison.Ordinal))
                {
                    throw new SvgXmlException(
                        "DOCTYPE and ENTITY declarations are refused: external entity expansion is an " +
                        "injection vector and this parser will not process them", open);
                }

                int close = source.IndexOf('>', open);
                if (close == -1) throw new SvgXmlException("unterminated declaration", open);
                index = close + 1;
                continue;
            }

            if (open + 1 < source.Length && source[open + 1] == '/')
            {
                int close = source.IndexOf('>', open);
                if (close == -1) throw new SvgXmlException("unterminated end tag", open);
                yield return new XmlEvent(XmlEventKind.End, LocalName(source[(open + 2)..close].Trim()));
                index = close + 1;
                continue;
            }

            int tagEnd = FindTagEnd(source, open);
            string raw = source[(open + 1)..tagEnd];
            bool selfClosing = raw.EndsWith('/');
            string body = selfClosing ? raw[..^1] : raw;

            Match nameMatch = ElementName().Match(body);
            if (!nameMatch.Success) throw new SvgXmlException("element has no name", open);

            yield return new XmlEvent(
                XmlEventKind.Start,
                LocalName(nameMatch.Groups["name"].Value),
                ParseAttributes(body[nameMatch.Groups["name"].Length..], open),
                selfClosing);

            index = tagEnd + 1;
        }
    }

    private static bool Matches(string source, int at, string prefix) =>
        at + prefix.Length <= source.Length && string.CompareOrdinal(source, at, prefix, 0, prefix.Length) == 0;

    /// <summary>
    /// Finds the <c>&gt;</c> closing a start tag, ignoring any inside a quoted
    /// value - a <c>&gt;</c> in a quoted value is legal, and scanning for the
    /// first one truncates the tag.
    /// </summary>
    private static int FindTagEnd(string source, int open)
    {
        char quote = '\0';
        for (int i = open + 1; i < source.Length; i++)
        {
            char c = source[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '>')
            {
                return i;
            }
        }

        throw new SvgXmlException("unterminated start tag", open);
    }

    /// <summary>
    /// Strips any namespace prefix.
    /// </summary>
    /// <remarks>
    /// Real SVG is inconsistent about prefixes - Inkscape and Illustrator emit
    /// <c>svg:</c>, and both <c>xlink:href</c> and plain <c>href</c> appear - and
    /// no element in the supported profile has a meaning that depends on its
    /// namespace. Comparing local names is what makes those files work.
    /// </remarks>
    private static string LocalName(string name)
    {
        int colon = name.IndexOf(':', StringComparison.Ordinal);
        return colon == -1 ? name : name[(colon + 1)..];
    }

    private static Dictionary<string, string> ParseAttributes(string text, int offset)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in AttributePattern().Matches(text))
        {
            string value = match.Groups["dq"].Success ? match.Groups["dq"].Value : match.Groups["sq"].Value;
            // A prefixed namespace declaration means nothing once every prefix is
            // stripped, and its local name - xmlns:xlink becomes xlink - would read
            // as an attribute. A plain xmlns is kept: HTML inside <foreignObject>
            // needs it if that subtree is ever handed to a rasterizer.
            if (match.Groups["name"].Value.StartsWith("xmlns:", StringComparison.Ordinal)) continue;
            attributes[LocalName(match.Groups["name"].Value)] = ResolveEntities(value);
        }

        // A bare attribute with no value is not valid XML; catching it surfaces
        // corrupt input rather than silently dropping a property.
        string leftover = AttributePattern().Replace(text, string.Empty).Trim();
        if (leftover.Length > 0 && !OnlySlashesAndSpace().IsMatch(leftover))
        {
            throw new SvgXmlException($"malformed attributes: \"{leftover}\"", offset);
        }

        return attributes;
    }
}
