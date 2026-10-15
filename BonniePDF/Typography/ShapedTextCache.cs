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
    private readonly record struct Entry((string Text, string Features) Key, IReadOnlyList<GlyphInfo> Glyphs);

    // Thread-safe: concurrent documents share a font. The lock covers only the cache; a miss
    // shapes outside it, and two threads missing the same key at once keep the first result.
    internal IReadOnlyList<GlyphInfo> Get(TtfFont font, string text, IReadOnlyDictionary<string, int>? features)
    {
        if (!Cacheable(text)) return Shape(font, text, features);
        var key = KeyOf(text, features);
        lock (_entries)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                _recent.Remove(existing);
                _recent.AddLast(existing);
                return existing.Value.Glyphs;
            }
        }
        var shaped = Shape(font, text, features);
        if (shaped.Count > MaxGlyphs) return shaped;
        var glyphs = shaped is System.Collections.ObjectModel.ReadOnlyCollection<GlyphInfo> readOnly ? readOnly : Array.AsReadOnly(shaped.ToArray());
        return Add(key, glyphs);
    }

    /// <summary>
    /// Records <paramref name="glyphs"/> as the shaping of <paramref name="text"/>, under the
    /// key <see cref="Get"/> would use in the current text flow. The caller guarantees they are
    /// exactly what shaping would return (see <see cref="ParagraphWidths.TryGetGlyphs"/>).
    /// </summary>
    internal void Seed(string text, IReadOnlyDictionary<string, int>? features, GlyphInfo[] glyphs)
    {
        if (!Cacheable(text) || glyphs.Length > MaxGlyphs) return;
        Add(KeyOf(text, features), Array.AsReadOnly(glyphs));
    }

    private static bool Cacheable(string text) =>
        !(text.Length > MaxTextLength || TextFlow.CurrentLine?.Paragraph.Length > MaxTextLength);

    private static (string Text, string Features) KeyOf(string text, IReadOnlyDictionary<string, int>? features)
    {
        string featureKey = features is null || features.Count == 0 ? string.Empty :
            string.Join(";", features.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f =>
                f.Key.Length.ToString(CultureInfo.InvariantCulture) + ":" + f.Key + "=" + f.Value.ToString(CultureInfo.InvariantCulture)));
        return (text, featureKey + "|" + TextFlow.CacheKey);
    }

    // Inserts glyphs under key unless another thread got there first; returns the entry kept.
    private IReadOnlyList<GlyphInfo> Add((string Text, string Features) key, IReadOnlyList<GlyphInfo> glyphs)
    {
        lock (_entries)
        {
            if (_entries.TryGetValue(key, out var raced)) return raced.Value.Glyphs;
            // A full cache evicts its oldest entry on every miss; reuse that entry's node.
            LinkedListNode<Entry>? node = null;
            while (_entries.Count >= MaxEntries || _glyphCount + glyphs.Count > MaxGlyphs)
            {
                node = _recent.First!;
                _glyphCount -= node.Value.Glyphs.Count;
                _entries.Remove(node.Value.Key);
                _recent.RemoveFirst();
            }
            var entry = new Entry(key, glyphs);
            if (node is null) node = new LinkedListNode<Entry>(entry);
            else node.Value = entry;
            _recent.AddLast(node);
            _entries.Add(key, node);
            _glyphCount += glyphs.Count;
        }
        return glyphs;
    }

    private static IReadOnlyList<GlyphInfo> Shape(TtfFont font, string text, IReadOnlyDictionary<string, int>? features) =>
        font.WithShapingFace((font, text, features), static (face, call) => EmbeddedTextShaping.Shape(call.font, face, call.text, call.features));
}
