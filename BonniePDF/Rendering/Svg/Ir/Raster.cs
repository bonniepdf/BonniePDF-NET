// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Rendering.Svg.Geometry;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>The region a raster covers, in root user space, and its size in pixels.</summary>
/// <param name="X">Left edge of the document's viewport.</param>
/// <param name="Y">Top edge of the document's viewport.</param>
/// <param name="Width">Viewport width.</param>
/// <param name="Height">Viewport height.</param>
/// <param name="PixelWidth">Raster width in pixels.</param>
/// <param name="PixelHeight">Raster height in pixels.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct RasterRegion(double X, double Y, double Width, double Height, int PixelWidth, int PixelHeight);

/// <summary>Standalone SVG snippets for the rasterizer fallback.</summary>
/// <remarks>
/// <para>
/// When the caller supplies a rasterizer and opts out of strict mode, an element
/// whose drawing the profile cannot reproduce - a filter, a mask, a marker, text no
/// supplied font covers - is handed to the rasterizer instead of being skipped.
/// This builds what it is handed: one self-contained SVG document that draws that
/// element, and only that element, exactly where it sits in the original.
/// </para>
/// <para>
/// Placement: the snippet's viewBox is the document's own viewport, and the element
/// sits under the matrix of everything above it - the whole viewport rather than
/// the element's box, because a blur or a drop shadow spreads past the box by an
/// amount only the filter knows. Inherited style: properties the element inherits
/// from ancestors the snippet does not contain are carried on a wrapper group;
/// non-inherited ones (opacity, clip-path) are not, because the IR's own group and
/// clip nodes still wrap the image. References: everything the element references
/// by <c>url(#id)</c> or <c>href</c>, transitively, is copied into <c>defs</c>.
/// </para>
/// <para>
/// Output must be byte-identical to TypeScript's <c>Ir/Raster.ts</c>: attribute
/// values are copied verbatim, and computed numbers go through
/// <see cref="FormatNumber"/>.
/// </para>
/// </remarks>
internal static partial class Raster
{
    /// <summary>
    /// Properties that inherit (SVG 2 and CSS), and so must reach the element from
    /// ancestors the snippet does not contain.
    /// </summary>
    private static readonly HashSet<string> Inherited = new(StringComparer.Ordinal)
    {
        "clip-rule", "color", "color-interpolation", "color-interpolation-filters", "color-rendering",
        "direction", "dominant-baseline", "fill", "fill-opacity", "fill-rule", "font", "font-family",
        "font-kerning", "font-size", "font-size-adjust", "font-stretch", "font-style", "font-variant",
        "font-weight", "image-rendering", "letter-spacing", "marker", "marker-end", "marker-mid",
        "marker-start", "paint-order", "shape-rendering", "stroke", "stroke-dasharray",
        "stroke-dashoffset", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-opacity",
        "stroke-width", "text-anchor", "text-rendering", "visibility", "word-spacing", "writing-mode",
    };

    private static readonly HashSet<string> ViewportAttributes =
        new(StringComparer.Ordinal) { "width", "height", "viewBox", "preserveAspectRatio", "xmlns" };

    /// <summary>A snippet drawing one element, with its ancestors' matrix and inherited style.</summary>
    public static string ElementSnippet(
        SvgElement element, IReadOnlyList<SvgElement> lineage, Matrix transform, RasterRegion region, SvgDom dom)
    {
        List<(string Property, string Value)> inherited = InheritedDeclarations(lineage);
        string? space = LastSpace(lineage);

        var body = new StringBuilder();
        Serialize(element, body);
        string defs = ReferencedDefinitions(element, inherited.Select(entry => entry.Value).ToList(), dom);

        var wrapper = new List<string>();
        if (!transform.IsIdentity)
        {
            double[] values = [transform.A, transform.B, transform.C, transform.D, transform.E, transform.F];
            wrapper.Add($"transform=\"matrix({string.Join(' ', values.Select(FormatNumber))})\"");
        }
        if (inherited.Count > 0)
        {
            wrapper.Add($"style=\"{EscapeAttribute(string.Join(';', inherited.Select(entry => $"{entry.Property}:{entry.Value}")))}\"");
        }
        if (space is not null) wrapper.Add($"xml:space=\"{EscapeAttribute(space)}\"");

        return SvgOpen(region, string.Empty) +
               (defs.Length > 0 ? $"<defs>{defs}</defs>" : string.Empty) +
               $"<g{string.Concat(wrapper.Select(attribute => " " + attribute))}>{body}</g>" +
               "</svg>";
    }

    /// <summary>
    /// A snippet drawing the whole document, for a problem that applies everywhere - a
    /// stylesheet. The root keeps its own attributes except those that fix the
    /// viewport, which are replaced so the image covers the same region as any other.
    /// </summary>
    public static string DocumentSnippet(SvgElement root, RasterRegion region)
    {
        var body = new StringBuilder();
        foreach (SvgContent entry in root.Content)
        {
            if (entry.Element is { } child) Serialize(child, body);
            else body.Append(EscapeText(entry.Text!));
        }

        var kept = new StringBuilder();
        foreach (KeyValuePair<string, string> attribute in root.Attributes)
        {
            if (ViewportAttributes.Contains(attribute.Key)) continue;
            kept.Append(' ').Append(AttributeName(attribute.Key)).Append("=\"").Append(EscapeAttribute(attribute.Value)).Append('"');
        }
        return SvgOpen(region, kept.ToString()) + body + "</svg>";
    }

    private static string SvgOpen(RasterRegion region, string extra) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{region.PixelWidth.ToString(CultureInfo.InvariantCulture)}\" " +
        $"height=\"{region.PixelHeight.ToString(CultureInfo.InvariantCulture)}\" " +
        $"viewBox=\"{string.Join(' ', new[] { region.X, region.Y, region.Width, region.Height }.Select(FormatNumber))}\" " +
        $"preserveAspectRatio=\"none\"{extra}>";

    /// <summary>Inherited declarations along the lineage, outermost first, later ones winning.</summary>
    /// <remarks>
    /// A property keeps the position of its first appearance when a later element
    /// overrides it - which is what a JavaScript <c>Map</c> does on the TypeScript side.
    /// </remarks>
    private static List<(string Property, string Value)> InheritedDeclarations(IReadOnlyList<SvgElement> lineage)
    {
        var order = new List<(string Property, string Value)>();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        void Set(string property, string value)
        {
            if (index.TryGetValue(property, out int at)) order[at] = (property, value);
            else
            {
                index[property] = order.Count;
                order.Add((property, value));
            }
        }

        foreach (SvgElement element in lineage)
        {
            foreach (KeyValuePair<string, string> attribute in element.Attributes)
            {
                if (Inherited.Contains(attribute.Key)) Set(attribute.Key, attribute.Value.Trim());
            }
            if (element.Attributes.TryGetValue("style", out string? inline) && inline.Length > 0)
            {
                foreach (string declaration in inline.Split(';'))
                {
                    int colon = declaration.IndexOf(':');
                    if (colon == -1) continue;
                    string property = declaration[..colon].Trim().ToLowerInvariant();
                    string value = declaration[(colon + 1)..].Trim();
                    if (value.Length > 0 && Inherited.Contains(property)) Set(property, value);
                }
            }
        }
        return order;
    }

    /// <summary>xml:space inherits too, and is an attribute rather than a property.</summary>
    private static string? LastSpace(IReadOnlyList<SvgElement> lineage)
    {
        string? space = null;
        foreach (SvgElement element in lineage)
        {
            if (element.Attributes.TryGetValue("space", out string? value)) space = value;
        }
        return space;
    }

    /// <summary>
    /// Every element referenced from the element's subtree or from the inherited
    /// values carried on its wrapper, transitively, serialized. Elements already in
    /// the snippet are not repeated - a duplicate id would make the reference ambiguous.
    /// </summary>
    private static string ReferencedDefinitions(SvgElement element, IReadOnlyList<string> inheritedValues, SvgDom dom)
    {
        var output = new StringBuilder();
        var included = new HashSet<SvgElement>(ReferenceEqualityComparer.Instance) { element };
        var pending = new List<string>();
        var queued = new HashSet<string>(StringComparer.Ordinal);
        void Queue(string id)
        {
            if (queued.Add(id)) pending.Add(id);
        }
        void Scan(SvgElement node)
        {
            foreach (string id in ReferencedIds(node)) Queue(id);
            foreach (SvgElement child in node.Children) Scan(child);
        }

        Scan(element);
        foreach (string value in inheritedValues)
        {
            foreach (string id in UrlIds(value)) Queue(id);
        }

        for (int i = 0; i < pending.Count; i++)
        {
            if (!dom.ById.TryGetValue(pending[i], out SvgElement? target)) continue;
            if (included.Contains(target) || ContainsAny(target, included)) continue;
            included.Add(target);
            Serialize(target, output);
            Scan(target);
        }
        return output.ToString();
    }

    private static bool ContainsAny(SvgElement element, HashSet<SvgElement> set)
    {
        foreach (SvgElement child in element.Children)
        {
            if (set.Contains(child) || ContainsAny(child, set)) return true;
        }
        return false;
    }

    /// <summary>Ids an element's own attributes point at: <c>url(#id)</c> anywhere, and <c>href="#id"</c>.</summary>
    private static List<string> ReferencedIds(SvgElement element)
    {
        var ids = new List<string>();
        foreach (KeyValuePair<string, string> attribute in element.Attributes)
        {
            if (string.Equals(attribute.Key, "href", StringComparison.Ordinal))
            {
                string trimmed = attribute.Value.Trim();
                if (trimmed.StartsWith('#') && trimmed.Length > 1) ids.Add(trimmed[1..]);
                continue;
            }
            ids.AddRange(UrlIds(attribute.Value));
        }
        return ids;
    }

    /// <summary>
    /// Ids in <c>url(#id)</c> references. Whitespace is spelled out rather than
    /// <c>\s</c>, which matches different characters in JavaScript and .NET.
    /// </summary>
    private static IEnumerable<string> UrlIds(string value) =>
        UrlReference().Matches(value).Select(match => match.Groups["id"].Value);

    [GeneratedRegex("url\\([ \\t\\n\\r\\f]*['\"]?#(?<id>[^'\") \\t\\n\\r\\f]+)['\"]?[ \\t\\n\\r\\f]*\\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlReference();

    private static void Serialize(SvgElement element, StringBuilder output)
    {
        output.Append('<').Append(element.Name);
        foreach (KeyValuePair<string, string> attribute in element.Attributes)
        {
            output.Append(' ').Append(AttributeName(attribute.Key)).Append("=\"").Append(EscapeAttribute(attribute.Value)).Append('"');
        }
        if (element.Content.Count == 0)
        {
            output.Append("/>");
            return;
        }
        output.Append('>');
        foreach (SvgContent entry in element.Content)
        {
            if (entry.Element is { } child) Serialize(child, output);
            else output.Append(EscapeText(entry.Text!));
        }
        output.Append("</").Append(element.Name).Append('>');
    }

    /// <summary>The reader strips namespace prefixes; <c>xml:space</c> is the one whose prefix matters.</summary>
    private static string AttributeName(string name) => string.Equals(name, "space", StringComparison.Ordinal) ? "xml:space" : name;

    private static string EscapeAttribute(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    private static string EscapeText(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// A computed number, as both implementations write it: rounded half up to six
    /// decimals, shortest round-trip form, no negative zero.
    /// </summary>
    /// <remarks>
    /// <c>Floor(x + 0.5)</c> because JavaScript's <c>Math.round</c> is half-up and
    /// .NET's <c>Math.Round</c> is half-to-even.
    /// </remarks>
    public static string FormatNumber(double value)
    {
        double rounded = Math.Floor(value * 1e6 + 0.5) / 1e6;
        return rounded == 0 ? "0" : rounded.ToString("R", CultureInfo.InvariantCulture);
    }
}
