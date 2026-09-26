// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using BonniePDF.Rendering.Svg.Xml;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>
/// A parsed element.
/// </summary>
/// <remarks>
/// Slice 1 needed no tree - the cascade and transform stack are just the stack of
/// open elements, so it resolved straight from the event stream. <c>use</c> and
/// gradients changed that: both reference elements by id, and a forward reference
/// is legal, so <c>&lt;use href="#x"&gt;</c> may appear before <c>#x</c> exists.
/// That needs random access, which needs a tree.
/// </remarks>
internal sealed class SvgElement
{
    /// <summary>Gets the element's local name, with any namespace prefix stripped.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the element's attributes, by local name.</summary>
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }

    /// <summary>Gets the child elements, in document order.</summary>
    public List<SvgElement> Children { get; } = [];

    /// <summary>
    /// Gets or sets character data directly inside this element, concatenated - for
    /// title, desc and style.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets character data and child elements interleaved, in document order.</summary>
    /// <remarks>
    /// <see cref="Text"/> loses where the characters sat relative to the children,
    /// which is harmless for a title and fatal for <c>text</c>:
    /// <c>a&lt;tspan&gt;b&lt;/tspan&gt;c</c> concatenates to "ac", placed before "b".
    /// Adjacent character data - text next to a CDATA section - is one entry.
    /// </remarks>
    public List<SvgContent> Content { get; } = [];
}

/// <summary>One entry of <see cref="SvgElement.Content"/>: character data or an element.</summary>
/// <param name="Text">The character data, when this entry is text.</param>
/// <param name="Element">The element, when this entry is an element.</param>
internal readonly record struct SvgContent(string? Text, SvgElement? Element);

/// <summary>A parsed document: its root, and an index of elements by id.</summary>
/// <param name="Root">The root element, or null for an empty document.</param>
/// <param name="ById">
/// Elements by <c>id</c>. First definition wins, matching how browsers resolve a
/// duplicate id rather than the last.
/// </param>
internal readonly record struct SvgDom(SvgElement? Root, IReadOnlyDictionary<string, SvgElement> ById);

/// <summary>Parses a document into a tree.</summary>
internal static class DomParser
{
    /// <summary>Parses <paramref name="source"/> into a tree plus an id index.</summary>
    public static SvgDom Parse(string source)
    {
        var byId = new Dictionary<string, SvgElement>(StringComparer.Ordinal);
        var stack = new List<SvgElement>();
        SvgElement? root = null;

        foreach (XmlEvent svgEvent in SvgXmlReader.Read(source))
        {
            if (svgEvent.Kind == XmlEventKind.Text)
            {
                if (stack.Count > 0)
                {
                    SvgElement current = stack[^1];
                    current.Text += svgEvent.Text;
                    if (current.Content.Count > 0 && current.Content[^1].Text is { } previous)
                        current.Content[^1] = new SvgContent(previous + svgEvent.Text, null);
                    else
                        current.Content.Add(new SvgContent(svgEvent.Text, null));
                }
                continue;
            }

            if (svgEvent.Kind == XmlEventKind.End)
            {
                // Tolerate a mismatched end tag by closing the innermost element,
                // as browsers do; a stricter reading discards documents that
                // render fine.
                if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var element = new SvgElement
            {
                Name = svgEvent.Name,
                Attributes = svgEvent.Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal),
            };

            if (element.Attributes.TryGetValue("id", out string? id) && id.Length > 0) byId.TryAdd(id, element);

            if (stack.Count > 0)
            {
                stack[^1].Children.Add(element);
                stack[^1].Content.Add(new SvgContent(null, element));
            }
            else
            {
                root ??= element;
            }

            if (!svgEvent.SelfClosing) stack.Add(element);
        }

        return new SvgDom(root, byId);
    }
}
