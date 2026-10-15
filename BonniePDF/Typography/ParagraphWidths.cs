using System;
using System.Collections.Generic;
using BonniePDF.Typography.Engine;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>
/// Widths of a paragraph's candidate lines, from one shaping of the whole paragraph.
/// </summary>
/// <remarks>
/// Word wrapping measures every candidate line, and measuring shapes the text: a paragraph of
/// n words used to be shaped n times, each time from its start. Shaping the paragraph once gives
/// the same width for a span whose two ends are boundaries the shaper reports as safe to break,
/// because shaping either side of a safe boundary separately gives the same glyphs. Any other
/// span is reported as unknown, and the caller measures it by shaping, as before. Widths are
/// therefore exactly what <see cref="FontMetrics.GetStringWidth"/> returns.
/// </remarks>
internal sealed class ParagraphWidths
{
    private readonly string _text;
    private readonly string _fontIdentifier;

    /// <summary>The font the paragraph was shaped with.</summary>
    internal TtfFont Font { get; }
    private readonly IReadOnlyDictionary<string, int>? _features;
    private readonly GlyphRun _glyphs;
    private readonly int[] _firstGlyph;
    private readonly long[] _advanceBefore;
    private readonly bool[] _safe;
    private readonly float _fontSize;
    private readonly int _unitsPerEm;

    // A shaping of text from a line start that is not a safe boundary (see TryGetWidth).
    private ParagraphWidths? _window;
    private int _windowStart = -1;

    private ParagraphWidths(string text, string fontIdentifier, IReadOnlyDictionary<string, int>? features,
        TtfFont font, GlyphRun glyphs, int[] firstGlyph, long[] advanceBefore, bool[] safe, float fontSize, int unitsPerEm)
    {
        Font = font;
        _glyphs = glyphs;
        _firstGlyph = firstGlyph;
        _text = text;
        _fontIdentifier = fontIdentifier;
        _features = features;
        _advanceBefore = advanceBefore;
        _safe = safe;
        _fontSize = fontSize;
        _unitsPerEm = unitsPerEm;
    }

    /// <summary>
    /// Shapes <paramref name="text"/> once, or returns null when its widths cannot come from one
    /// shaping: text outside printable ASCII (it takes other shaping paths), a font measured by
    /// width table, a font that does not cover the text, or a paragraph the shaper rejects.
    /// </summary>
    internal static ParagraphWidths? TryCreate(string text, string fontIdentifier, float fontSize, IReadOnlyDictionary<string, int>? features)
    {
        if (text.Length < 2) return null;
        foreach (char c in text)
            if (c < ' ' || c > '~') return null;
        TtfFont? font = FontMetrics.ResolveShapingFont(fontIdentifier, out _);
        if (font is null || !TextRunShaper.PrimaryCovers(text, font)) return null;

        GlyphRun glyphs;
        try
        {
            glyphs = font.Shape(text, features);
        }
        catch (Exception e) when (e is System.IO.InvalidDataException or NotSupportedException)
        {
            // A limit or an unsupported feature on the whole paragraph; shorter lines may still shape.
            return null;
        }

        var firstGlyph = new int[text.Length + 1];
        var advanceBefore = new long[text.Length + 1];
        var safe = new bool[text.Length + 1];
        long total = 0;
        int previousCluster = -1;
        for (int i = 0; i < glyphs.Count; i++)
        {
            int cluster = glyphs.Cluster(i);
            if (cluster < previousCluster || cluster >= text.Length) return null;
            if (cluster != previousCluster)
            {
                // The first glyph of each cluster carries whether a break before it is safe.
                advanceBefore[cluster] = total;
                firstGlyph[cluster] = i;
                safe[cluster] = cluster == 0 || !glyphs.UnsafeToBreak(i);
                previousCluster = cluster;
            }
            total += glyphs.AdvanceX(i);
        }
        advanceBefore[text.Length] = total;
        firstGlyph[text.Length] = glyphs.Count;
        safe[text.Length] = true;
        return new ParagraphWidths(text, fontIdentifier, features, font, glyphs, firstGlyph, advanceBefore, safe, fontSize, font.UnitsPerEm);
    }

    /// <summary>
    /// The width of <c>text[start..end)</c>, as <see cref="FontMetrics.GetStringWidth"/> measures
    /// it, when it can be read from a shaping; otherwise false.
    /// </summary>
    /// <remarks>
    /// A span between two safe boundaries is read from the paragraph's shaping. A line that
    /// starts at an unsafe boundary (a kerned space, say) gets its own shaping of the text from
    /// that start, the same as the paragraph's but beginning there, and its candidate ends are
    /// read from that under the same rule. Only an unsafe end leaves the span unknown.
    /// </remarks>
    internal bool TryGetWidth(int start, int end, out float width)
    {
        width = 0;
        if (start >= end) return false;
        if (_safe[start])
        {
            if (!_safe[end]) return false;
            // The same arithmetic as GetKernedStringWidth: an exact integer sum, scaled in double.
            width = (float)((double)(_advanceBefore[end] - _advanceBefore[start]) * _fontSize / _unitsPerEm);
            return true;
        }
        ParagraphWidths? window = WindowAt(start, end);
        return window != null && window.TryGetWidth(0, end - start, out width);
    }

    /// <summary>
    /// The glyphs of <c>text[start..end)</c> shaped on its own, with clusters relative to
    /// <paramref name="start"/>, when they can be read from a shaping under the same rule as
    /// <see cref="TryGetWidth"/>; otherwise null.
    /// </summary>
    internal GlyphRun? TryGetGlyphs(int start, int end)
    {
        if (start >= end) return null;
        if (!_safe[start]) return WindowAt(start, end)?.TryGetGlyphs(0, end - start);
        if (!_safe[end]) return null;
        // A cluster's source length runs to the next cluster, which at the line's end is `end`
        // in both shapings, so every field carries over with the cluster rebased.
        return _glyphs.Slice(_firstGlyph[start], _firstGlyph[end], -start);
    }

    // The shaping of text from an unsafe line start, covering end; created once per line start.
    private ParagraphWidths? WindowAt(int start, int end)
    {
        if (start != _windowStart)
        {
            // Long enough for a few lines: a later end falls back, or starts a new window.
            int length = Math.Min(_text.Length - start, Math.Max(256, 4 * (end - start)));
            _window = TryCreate(_text.Substring(start, length), _fontIdentifier, _fontSize, _features);
            _windowStart = start;
        }
        return _window != null && end - start <= _window._text.Length ? _window : null;
    }
}
