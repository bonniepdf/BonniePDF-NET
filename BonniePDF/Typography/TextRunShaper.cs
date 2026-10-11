using System;
using System.Buffers;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>A font the shaper may select glyphs from.</summary>
internal sealed class ShaperFont
{
    /// <summary>Opaque identifier the caller uses to resolve a resource name + tracker.</summary>
    public required string FontIdentifier { get; init; }

    /// <summary>The parsed font, queried for glyph coverage.</summary>
    public required TtfFont Font { get; init; }
}

/// <summary>A maximal substring renderable by a single candidate font.</summary>
internal sealed class ShapedRun
{
    /// <summary>Index into the candidate list passed to <see cref="TextRunShaper.ShapeTextRuns"/> (0 = primary).</summary>
    public int FontIndex { get; init; }

    /// <summary>The substring rendered by candidate <see cref="FontIndex"/>.</summary>
    public required string Text { get; set; }
}

/// <summary>Result of <see cref="TextRunShaper.ShapeTextRuns"/>.</summary>
internal sealed class ShapeResult
{
    /// <summary>The runs, in source order, each renderable by a single font.</summary>
    public IList<ShapedRun> Runs { get; } = new List<ShapedRun>();

    /// <summary>Code points no candidate font could render (dropped from output).</summary>
    public IList<int> Unsupported { get; } = new List<int>();
}

/// <summary>
/// Splits text into runs, each renderable by a single font, preferring the
/// primary font (<c>candidates[0]</c>) and falling back to later candidates for
/// code points the primary lacks.
/// </summary>
internal static class TextRunShaper
{
    /// <summary>
    /// Splits <paramref name="text"/> into runs, each renderable by a single
    /// font, preferring the primary font (<c>candidates[0]</c>) and falling back
    /// to later candidates for code points the primary lacks.
    /// </summary>
    /// <remarks>
    /// Control and default-ignorable code points are dropped (see
    /// <see cref="TextEncoding.IsRenderableCodePoint"/>). Code points no candidate
    /// covers are also dropped and reported in <see cref="ShapeResult.Unsupported"/>,
    /// so the caller surfaces a diagnostic instead of emitting a <c>.notdef</c>
    /// reference that PDF/A-2 (6.2.11.8) and PDF/UA-1 (7.21.8) forbid.
    ///
    /// When the primary font covers every code point the result is a single run
    /// with <c>FontIndex == 0</c>, so callers that don't need fallback behave
    /// exactly as before.
    /// </remarks>
    internal static bool PrimaryCovers(string text, TtfFont font)
    {
        var rest = text.AsSpan();
        while (!rest.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(rest, out var ch, out int length) != OperationStatus.Done ||
                !TextEncoding.IsRenderableCodePoint(ch.Value) || font.GetGlyphId(ch.Value) == 0) return false;
            rest = rest[length..];
        }
        return true;
    }

    public static ShapeResult ShapeTextRuns(string text, IReadOnlyList<ShaperFont> candidates)
    {
        var result = new ShapeResult();
        if (string.IsNullOrEmpty(text) || candidates.Count == 0) return result;

        // No fallback choice can change a cluster when the primary covers every
        // printable scalar. Keep the original string instead of rebuilding it.
        if (PrimaryCovers(text, candidates[0].Font))
        {
            result.Runs.Add(new ShapedRun { FontIndex = 0, Text = text });
            return result;
        }

        var buffer = new StringBuilder();
        int currentFont = -1;
        void Flush()
        {
            if (currentFont >= 0) result.Runs.Add(new ShapedRun { FontIndex = currentFont, Text = buffer.ToString() });
            buffer.Clear();
        }
        void Append(int chosen, string source)
        {
            if (currentFont != chosen) { Flush(); currentFont = chosen; }
            buffer.Append(source);
        }
        var bounds=Typography.Engine.Graphemes.Boundaries(text);
        for(int b=0;b+1<bounds.Count;b++)
        {
            var chars=text.Substring(bounds[b],bounds[b+1]-bounds[b]).EnumerateRunes().Where(c=>TextEncoding.IsRenderableCodePoint(c.Value)).ToArray();
            if(chars.Length==0)continue;
            int whole=-1;
            for(int i=0;i<candidates.Count;i++)if(chars.All(c=>candidates[i].Font.GetGlyphId(c.Value)!=0)){whole=i;break;}
            if(whole>=0){Append(whole,candidates[whole].Font.HasColour ? text.Substring(bounds[b],bounds[b+1]-bounds[b]) : string.Concat(chars.Select(c=>c.ToString())));continue;}
            foreach(var ch in chars) {int chosen=-1;for(int i=0;i<candidates.Count;i++)if(candidates[i].Font.GetGlyphId(ch.Value)!=0){chosen=i;break;}if(chosen>=0)Append(chosen,ch.ToString());else result.Unsupported.Add(ch.Value);}
        }
        Flush();
        return result;
    }
}
