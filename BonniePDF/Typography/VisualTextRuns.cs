using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BonniePDF.Typography.Engine;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>
/// One piece of a line drawn in more than one font, in visual (left-to-right painting) order.
/// <see cref="Run"/> is the index of the font run it belongs to. For a run with a font,
/// <see cref="Text"/> is the piece's logical text and <see cref="Glyphs"/> its shaped glyphs in
/// visual order, clusters relative to <see cref="Text"/>. For a run without one (a standard font
/// the viewer draws), <see cref="Glyphs"/> is null and <see cref="Text"/> is already in visual order.
/// </summary>
internal readonly record struct VisualTextPiece(int Run, string Text, GlyphRun? Glyphs);

/// <summary>Orders the font runs of one bidirectional line for painting (spec 22, fallback-font boundaries).</summary>
internal static class VisualTextRuns
{
    /// <summary>
    /// Shaping each font run on its own resolves each as if it were a paragraph: the pieces are then
    /// painted in logical order and a neutral at a run's edge (a space, a comma) takes the run's own
    /// direction, so spaces beside a fallback run land on the wrong side. Instead the line is resolved
    /// once - bidi levels over the whole line, in the flow's direction - with each grapheme shaped in
    /// its run's face, and the pieces come back in visual order (UBA rule L2), split wherever the run
    /// changes.
    /// </summary>
    /// <returns>
    /// Null when nothing needs reordering (one run, or a line with no bidi content), or when the line
    /// cannot be resolved this way (no run has a font, or a script without a shaper), in which case
    /// the caller paints the runs in logical order as before.
    /// </returns>
    internal static List<VisualTextPiece>? Order(IReadOnlyList<string> runTexts, IReadOnlyList<TtfFont?> fonts,
        IReadOnlyDictionary<string, int>? features)
    {
        if (runTexts.Count < 2) return null;
        string text = string.Concat(runTexts);
        if (text.Length == 0 || !(TextFlow.RightToLeft || TextFlow.NeedsBidi(text))) return null;
        TtfFont? anyFont = fonts.FirstOrDefault(font => font is not null);
        if (anyFont is null) return null;

        var ends = new int[runTexts.Count];
        int offset = 0;
        for (int i = 0; i < runTexts.Count; i++) ends[i] = offset += runTexts[i].Length;
        int RunAt(int index)
        {
            int run = 0;
            while (ends[run] <= index) run++;
            return run;
        }

        // Each font's face is borrowed for the whole line; a run without a font is ordered with any face of the line.
        var rented = new List<(TtfFont Font, FontFace Face)>();
        FontFace FaceOf(TtfFont font)
        {
            foreach (var (owner, face) in rented)
                if (ReferenceEquals(owner, font)) return face;
            FontFace rentedFace = font.RentShapingFace();
            rented.Add((font, rentedFace));
            return rentedFace;
        }
        try
        {
            var faces = fonts.Select(font => font is null ? null : FaceOf(font)).ToArray();
            FontFace fallback = FaceOf(anyFont);
            ShapeOptions? options = features is null || features.Count == 0
                ? null
                : new ShapeOptions(features: features.Select(f => new ShapeFeature(f.Key, f.Value)));
            IReadOnlyList<ShapedTextItem> items;
            try
            {
                items = TextItemizer.Resolve(text, (start, _, _, _) => faces[RunAt(start)] ?? fallback,
                    TextFlow.RightToLeft ? "rtl" : "ltr").ShapeLine(0, text.Length, options);
            }
            catch (Exception error) when (error is NotSupportedException or ArgumentException or InvalidDataException)
            {
                // A script without a shaper, or a line past the shaping limits.
                return null;
            }

            var pieces = new List<VisualTextPiece>();
            foreach (ShapedTextItem item in items)
            {
                var glyphs = item.Glyphs;
                int first = 0;
                while (first < glyphs.Count)
                {
                    int run = RunAt(glyphs[first].Cluster);
                    int last = first + 1;
                    while (last < glyphs.Count && RunAt(glyphs[last].Cluster) == run) last++;
                    pieces.Add(Piece(text, glyphs, first, last, run, faces[run] is not null));
                    first = last;
                }
            }
            return pieces;
        }
        finally
        {
            foreach (var (font, face) in rented) font.ReturnShapingFace(face);
        }
    }

    private static VisualTextPiece Piece(string text, IReadOnlyList<GlyphInfo> glyphs, int first, int last, int run, bool shaped)
    {
        if (!shaped)
        {
            // Each source cluster once, in the order its glyphs are painted.
            var visual = new System.Text.StringBuilder();
            int prior = -1;
            for (int i = first; i < last; i++)
            {
                GlyphInfo g = glyphs[i];
                if (g.Cluster == prior) continue;
                visual.Append(text, g.Cluster, g.SourceLength);
                prior = g.Cluster;
            }
            return new VisualTextPiece(run, visual.ToString(), null);
        }
        int start = int.MaxValue, end = 0;
        for (int i = first; i < last; i++)
        {
            start = Math.Min(start, glyphs[i].Cluster);
            end = Math.Max(end, glyphs[i].Cluster + glyphs[i].SourceLength);
        }
        var builder = new GlyphRun.Builder(last - first);
        for (int i = first; i < last; i++)
        {
            GlyphInfo g = glyphs[i];
            builder.Add(g.Id, g.Cluster - start, g.AdvanceX, g.AdvanceY, g.OffsetX, g.OffsetY,
                g.ComponentCount, g.IsMark, g.IsLigature, g.SourceLength, g.UnsafeToBreak, g.ComponentIndex);
        }
        return new VisualTextPiece(run, text.Substring(start, end - start), builder.Build());
    }
}
