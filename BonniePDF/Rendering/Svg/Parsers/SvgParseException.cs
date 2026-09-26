// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>Thrown when an attribute value cannot be parsed. Carries enough context to locate it.</summary>
internal sealed class SvgParseException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="offset">Character offset into <paramref name="source"/>.</param>
    /// <param name="source">The attribute value being parsed.</param>
    public SvgParseException(string message, int offset, string source)
        : base($"{message} (near \"{Excerpt(source, offset)}\")")
    {
        Offset = offset;
        SourceText = source;
    }

    /// <summary>Gets the character offset the failure was detected at.</summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the value being parsed. Named <c>SourceText</c> rather than
    /// <c>Source</c> because <see cref="Exception.Source"/> already exists and
    /// means something else - the application or object that raised the error.
    /// </summary>
    public string SourceText { get; }

    private static string Excerpt(string source, int offset)
    {
        int start = Math.Max(0, offset - 16);
        int end = Math.Min(source.Length, offset + 16);
        return source[start..end];
    }
}
