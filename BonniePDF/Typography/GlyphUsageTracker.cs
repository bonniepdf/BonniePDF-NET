using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>
/// Tracks which characters/glyphs are used in a document.
/// Used for generating ToUnicode CMap and future font subsetting.
/// Supports the full Unicode range (BMP + supplementary planes).
/// </summary>
internal class GlyphUsageTracker
{
    private readonly Dictionary<ushort,string> _sources = new();
    private readonly Dictionary<ushort,ushort> _cidGlyphs = new();
    private readonly Dictionary<(ushort Glyph,string Source),ushort> _sourceCids = new();
    private int _nextCid;
    internal IReadOnlyDictionary<ushort,string> GlyphSources => _sources;
    internal IReadOnlyDictionary<ushort,ushort> CidToGlyphMap {
        get {var map=_usedGlyphs.ToDictionary(g=>g,g=>g);foreach(var e in _cidGlyphs)map[e.Key]=e.Value;return map;}
    }
    internal IReadOnlyList<ushort> MarkShaped(string text, IReadOnlyList<Typography.Engine.GlyphInfo> glyphs)
    {
        var codes=new List<ushort>();
        for(int i=0;i<glyphs.Count;) {
            var first=glyphs[i];int end=i+1;
            while(end<glyphs.Count && glyphs[end].Cluster==first.Cluster)end++;
            string clusterSource = text.Substring(first.Cluster, first.SourceLength);
            var chars = end == i + 1 ? null : clusterSource.EnumerateRunes().Select(r => r.ToString()).ToArray();
            for(int j=i;j<end;j++) {
                ushort id=checked((ushort)glyphs[j].Id);_usedGlyphs.Add(id);
                string source = chars is null ? clusterSource : j == end - 1 ? string.Concat(chars.Skip(j - i)) : j - i < chars.Length ? chars[j - i] : string.Empty;
                if(!_sourceCids.TryGetValue((id,source),out ushort code)) {
                    int value=_sources.ContainsKey(id)?Math.Max(_nextCid,_font.NumberOfGlyphs):id;
                    if(value>ushort.MaxValue)throw new InvalidOperationException("PDF font character-code capacity exceeded.");
                    code=(ushort)value;if(value>=_font.NumberOfGlyphs)_nextCid=value+1;
                    _sources.Add(code,source);_sourceCids.Add((id,source),code);_cidGlyphs.Add(code,id);
                }
                codes.Add(code);
            }
            i=end;
        }
        return codes;
    }
    private readonly TtfFont _font;
    private readonly HashSet<int> _usedCodepoints = new();
    private readonly HashSet<ushort> _usedGlyphs = new();

    /// <summary>
    /// Creates a new glyph usage tracker for the specified font
    /// </summary>
    public GlyphUsageTracker(TtfFont font)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));

        // Always include .notdef glyph (GID 0)
        _usedGlyphs.Add(0);
    }

    /// <summary>
    /// Gets the set of used Unicode codepoints
    /// </summary>
    public IReadOnlySet<int> UsedCodepoints => _usedCodepoints;

    /// <summary>
    /// Gets the set of used glyph IDs
    /// </summary>
    public IReadOnlySet<ushort> UsedGlyphIds => _usedGlyphs;

    /// <summary>
    /// Gets the number of unique codepoints used
    /// </summary>
    public int UsedCharacterCount => _usedCodepoints.Count;

    /// <summary>
    /// Gets the number of unique glyphs used
    /// </summary>
    public int UsedGlyphCount => _usedGlyphs.Count;

    /// <summary>
    /// Marks a Unicode codepoint as used
    /// </summary>
    public void MarkUsed(int codepoint)
    {
        if (_usedCodepoints.Add(codepoint))
        {
            ushort gid = _font.GetGlyphId(codepoint);
            _usedGlyphs.Add(gid);
        }
    }

    /// <summary>
    /// Marks a BMP character as used
    /// </summary>
    public void MarkUsed(char c) => MarkUsed((int)c);

    /// <summary>
    /// Marks all characters in a string as used (handles surrogate pairs via Rune enumeration)
    /// </summary>
    public void MarkUsed(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        MarkUsed(text, _font.Shape(text));
    }

    internal IReadOnlyList<ushort> MarkUsed(string text, IReadOnlyList<Typography.Engine.GlyphInfo> glyphs)
    {
        var codes = MarkShaped(text, glyphs);
        foreach (Rune rune in text.EnumerateRunes())
            MarkUsed(rune.Value);
        return codes;
    }

    /// <summary>
    /// Returns mapping of used codepoints to their glyph IDs
    /// </summary>
    public IDictionary<int, ushort> GetUsedCharToGlyphMap()
    {
        return _usedCodepoints.ToDictionary(cp => cp, cp => _font.GetGlyphId(cp));
    }
}
