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
    public static ShapeResult ShapeTextRuns(string text, IReadOnlyList<ShaperFont> candidates)
    {
        var result = new ShapeResult();
        if (string.IsNullOrEmpty(text) || candidates.Count == 0) return result;

        ShapedRun? current=null;
        void Append(int chosen,string source) { if(current is not null && current.FontIndex==chosen)current.Text+=source;else {current=new ShapedRun {FontIndex=chosen,Text=source};result.Runs.Add(current);} }
        var bounds=Typography.Engine.Graphemes.Boundaries(text);
        for(int b=0;b+1<bounds.Count;b++)
        {
            var chars=text.Substring(bounds[b],bounds[b+1]-bounds[b]).EnumerateRunes().Where(c=>TextEncoding.IsRenderableCodePoint(c.Value)).ToArray();
            if(chars.Length==0)continue;
            int whole=-1;
            for(int i=0;i<candidates.Count;i++)if(chars.All(c=>candidates[i].Font.GetGlyphId(c.Value)!=0)){whole=i;break;}
            if(whole>=0){Append(whole,string.Concat(chars.Select(c=>c.ToString())));continue;}
            foreach(var ch in chars) {int chosen=-1;for(int i=0;i<candidates.Count;i++)if(candidates[i].Font.GetGlyphId(ch.Value)!=0){chosen=i;break;}if(chosen>=0)Append(chosen,ch.ToString());else result.Unsupported.Add(ch.Value);}
        }
        return result;
    }
}
