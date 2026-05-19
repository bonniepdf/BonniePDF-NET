using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Typography;

/// <summary>
/// Tracks which characters/glyphs are used in a document.
/// Used for generating ToUnicode CMap and future font subsetting.
/// Supports the full Unicode range (BMP + supplementary planes).
/// </summary>
public class GlyphUsageTracker
{
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

        foreach (Rune rune in text.EnumerateRunes())
            MarkUsed(rune.Value);
    }

    /// <summary>
    /// Returns mapping of used codepoints to their glyph IDs
    /// </summary>
    public IDictionary<int, ushort> GetUsedCharToGlyphMap()
    {
        return _usedCodepoints.ToDictionary(cp => cp, cp => _font.GetGlyphId(cp));
    }
}
