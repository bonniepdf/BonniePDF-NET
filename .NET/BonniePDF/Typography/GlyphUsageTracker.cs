using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF.Typography;

/// <summary>
/// Tracks which characters/glyphs are used in a document.
/// Used for generating ToUnicode CMap and future font subsetting.
/// </summary>
public class GlyphUsageTracker
{
    private readonly TtfFont _font;
    private readonly HashSet<char> _usedChars = new();
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
    /// Gets the set of used Unicode characters
    /// </summary>
    public IReadOnlySet<char> UsedCharacters => _usedChars;

    /// <summary>
    /// Gets the set of used glyph IDs
    /// </summary>
    public IReadOnlySet<ushort> UsedGlyphIds => _usedGlyphs;

    /// <summary>
    /// Gets the number of unique characters used
    /// </summary>
    public int UsedCharacterCount => _usedChars.Count;

    /// <summary>
    /// Gets the number of unique glyphs used
    /// </summary>
    public int UsedGlyphCount => _usedGlyphs.Count;

    /// <summary>
    /// Marks a character as used
    /// </summary>
    public void MarkUsed(char c)
    {
        if (_usedChars.Add(c))
        {
            ushort gid = _font.GetGlyphId(c);
            _usedGlyphs.Add(gid);
        }
    }

    /// <summary>
    /// Marks all characters in a string as used
    /// </summary>
    public void MarkUsed(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        foreach (char c in text) MarkUsed(c);
    }

    /// <summary>
    /// Returns mapping of used characters to their glyph IDs
    /// </summary>
    public Dictionary<char, ushort> GetUsedCharToGlyphMap()
    {
        return _usedChars.ToDictionary(c => c, c => _font.GetGlyphId(c));
    }
}