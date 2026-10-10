using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BonniePDF.Typography.Engine;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

// One cache per immutable font. Bound both entry count and glyph count so unique
// text and long documents cannot accumulate unbounded process-wide state.
internal sealed class ShapedTextCache
{
    private const int MaxEntries = 256;
    private const int MaxGlyphs = 16384;
    private const int MaxTextLength = 512;
    private readonly Dictionary<(string Text, string Features), LinkedListNode<Entry>> _entries = new();
    private readonly LinkedList<Entry> _recent = new();
    private int _glyphCount;
    private sealed record Entry((string Text, string Features) Key, IReadOnlyList<GlyphInfo> Glyphs);

    internal IReadOnlyList<GlyphInfo> Get(TtfFont font, string text, IReadOnlyDictionary<string, int>? features)
    {
        if (text.Length > MaxTextLength) return EmbeddedTextShaping.Shape(font, text, features);
        string featureKey = features is null || features.Count == 0 ? string.Empty :
            string.Join(";", features.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f =>
                f.Key.Length.ToString(CultureInfo.InvariantCulture) + ":" + f.Key + "=" + f.Value.ToString(CultureInfo.InvariantCulture)));
        var key = (text, featureKey);
        if (_entries.TryGetValue(key, out var existing))
        {
            _recent.Remove(existing);
            _recent.AddLast(existing);
            return existing.Value.Glyphs;
        }
        var shaped = EmbeddedTextShaping.Shape(font, text, features);
        if (shaped.Count > MaxGlyphs) return shaped;
        var glyphs = Array.AsReadOnly(shaped.ToArray());
        while (_entries.Count >= MaxEntries || _glyphCount + glyphs.Count > MaxGlyphs)
        {
            var oldest = _recent.First!;
            _glyphCount -= oldest.Value.Glyphs.Count;
            _entries.Remove(oldest.Value.Key);
            _recent.RemoveFirst();
        }
        _entries.Add(key, _recent.AddLast(new Entry(key, glyphs)));
        _glyphCount += glyphs.Count;
        return glyphs;
    }
}
