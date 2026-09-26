// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF.Rendering.Svg.Fonts;

/// <summary>An <see cref="IFontProvider"/> over a set of registered faces.</summary>
/// <remarks>
/// <para>
/// Family names match exactly, ignoring case - there is no "Helvetica is close to
/// Arial" step, because a guessed font is a silent substitution. Generic families
/// (<c>sans-serif</c> and the rest) match only if the caller maps them to a
/// registered family with <see cref="SetGeneric"/>.
/// </para>
/// <para>
/// Within a family, the face is chosen by the CSS Fonts font-matching algorithm (CSS
/// Fonts Level 4, 5.2): style narrows first, then weight. That is a defined
/// procedure, not a heuristic - it is how a browser picks Liberation Sans Bold for
/// <c>font-weight: 600</c> when no 600 exists.
/// </para>
/// </remarks>
internal sealed class FontSet : IFontProvider
{
    private readonly record struct Entry(IFontFace Face, int Weight, bool Italic);

    private readonly Dictionary<string, List<Entry>> _families = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _generics = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers a face under a family name.</summary>
    public FontSet Add(string family, IFontFace face, int weight = 400, bool italic = false)
    {
        if (!_families.TryGetValue(family, out List<Entry>? entries))
        {
            entries = [];
            _families[family] = entries;
        }
        entries.Add(new Entry(face, weight, italic));
        return this;
    }

    /// <summary>Maps a CSS generic family - sans-serif, serif, monospace, ... - to a registered family.</summary>
    public FontSet SetGeneric(string generic, string family)
    {
        _generics[generic] = family;
        return this;
    }

    /// <inheritdoc />
    public IFontFace? Resolve(string family, int weight, bool italic)
    {
        string key = _generics.TryGetValue(family, out string? mapped) ? mapped : family;
        if (!_families.TryGetValue(key, out List<Entry>? entries) || entries.Count == 0) return null;
        return MatchWeight(MatchStyle(entries, italic), weight).Face;
    }

    /// <summary>Faces of the closest style: italic falls back to normal, and normal to italic.</summary>
    private static List<Entry> MatchStyle(List<Entry> entries, bool italic)
    {
        List<Entry> exact = entries.Where(entry => entry.Italic == italic).ToList();
        return exact.Count > 0 ? exact : entries;
    }

    /// <summary>
    /// CSS font-weight matching. For a desired weight between 400 and 500, weights up to
    /// 500 are tried ascending, then lighter ones descending, then heavier ones
    /// ascending. Below 400, lighter first then heavier; above 500, heavier first then
    /// lighter.
    /// </summary>
    private static Entry MatchWeight(List<Entry> entries, int desired)
    {
        foreach (Entry entry in entries)
        {
            if (entry.Weight == desired) return entry;
        }

        // A stable sort, so equal weights keep registration order as in TypeScript.
        List<Entry> ascending = entries.OrderBy(entry => entry.Weight).ToList();
        List<Entry> lighter = ascending.Where(entry => entry.Weight < desired).Reverse().ToList();
        List<Entry> heavier = ascending.Where(entry => entry.Weight > desired).ToList();

        if (desired is >= 400 and <= 500)
        {
            return heavier.Where(entry => entry.Weight <= 500)
                .Concat(lighter)
                .Concat(heavier.Where(entry => entry.Weight > 500))
                .First();
        }
        return (desired < 400 ? lighter.Concat(heavier) : heavier.Concat(lighter)).First();
    }
}
