// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    // Single-font default shaping with explicit OpenType selection and UTF-16 sources.
    internal static class Shaper
    {
        internal static ShapedRun Shape(FontFace face, string text, ShapeOptions options = null, bool itemized = false)
        {
            if (face == null) throw new ArgumentNullException("face");
            if (text == null) throw new ArgumentNullException("text");
            var settings = options ?? face.DefaultOptions;
            var limits = settings.Limits ?? face.Limits;
            if (text.Length > limits.MaxTextLength)
                throw new ArgumentException("Text exceeds the configured UTF-16 limit.", "text");
            settings.ValidateText(text);
            bool hebrew = settings.Script == "hebr", arabic = settings.Script == "arab", nativeRtl = hebrew || arabic;
            var run = face.BeginRun();
            // Printable ASCII has no canonical decomposition or multi-scalar
            // clusters. Keep the same lookup pipeline and all bounded error paths.
            bool ascii = settings.Script == "latn" && settings.Direction == TextDirection.LeftToRight &&
                text.Length <= limits.MaxGlyphs && text.Length * 3 <= limits.MaxOperations;
            for (int i = 0; ascii && i < text.Length; i++) ascii = text[i] >= 32 && text[i] <= 126;
            if (ascii)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int id = face.AsciiGlyph(text[i]);
                    if (id == 0) throw new NotSupportedException("Font has no glyph for U+" + ((int)text[i]).ToString("X4") + ".");
                    run.Add(face.RentGlyph(id, text[i], i));
                }
            }
            else
            {
                var input = EmojiClusters.Scalars(text, nativeRtl || itemized, arabic);
                var tokens = new List<SourceScalar>();
                int offset = 0;
                foreach (int scalar in input)
                {
                    int end = offset + (scalar > 65535 ? 2 : 1);
                    tokens.Add(new SourceScalar(scalar, offset, end)); offset = end;
                }
                tokens = new CanonicalNormalizer(limits, face.Covers, hebrew ? (Func<int, int>)HebrewShaper.CombiningClass : null,
                    hebrew ? (Func<int, bool>)(cp => cp >= 0xfb1d && cp <= 0xfb4e && face.Covers(cp)) : null, hebrew ? 32 : int.MaxValue).Normalize(tokens);
                if (hebrew) tokens = HebrewShaper.Compose(face, tokens);
                if (arabic) tokens = ArabicShaper.Reorder(face, tokens);
                var scalars = new List<int>();
                foreach (var token in tokens)
                {
                    int scalar = token.Scalar;
                    if (settings.Direction == TextDirection.RightToLeft)
                    {
                        int mirrored = UnicodeData.Mirror(scalar);
                        if (face.Covers(mirrored)) scalar = mirrored;
                    }
                    scalars.Add(scalar);
                }
                foreach (var cluster in EmojiClusters.Split(scalars, !arabic))
                {
                    int sourceOffset = tokens[cluster.Start].Start;
                    for (int i = cluster.Start + 1; i < cluster.End; i++) sourceOffset = Math.Min(sourceOffset, tokens[i].Start);
                    AppendCluster(face, scalars, cluster, sourceOffset, run, tokens, nativeRtl);
                }
            }
            if ((!nativeRtl && settings.Direction == TextDirection.RightToLeft) || (nativeRtl && settings.Direction == TextDirection.LeftToRight))
            {
                run.Reverse();
                for (int first = 0; first < run.Count;)
                {
                    int end = first + 1;
                    while (end < run.Count && run[end].Cluster == run[first].Cluster) end++;
                    run.Reverse(first, end - first); first = end;
                }
            }
            if (arabic) ArabicShaper.Join(run);
            var program = face.Program(options);
            var adjustments = PositionRun(program.Substitution, program.Positioning, run, face.Font, nativeRtl, arabic);
            if (nativeRtl) { run.Reverse(); Array.Reverse(adjustments); }
            bool wide = false;
            for (int i = 0; i < run.Count && !wide; i++)
                wide = GlyphRun.NeedsWide(adjustments[i].YAdvance, adjustments[i].YPlacement, run[i].ComponentCount, run[i].MarkComponent);
            var output = new GlyphRun(run.Count, wide);
            bool ordered = true;
            for (int i = 1; i < run.Count; i++)
                if (run[i].Cluster < run[i - 1].Cluster) { ordered = false; break; }
            if (ordered)
            {
                // Logical LTR clusters are usually already ordered. Walk each
                // equal-cluster group once instead of building per-glyph trees,
                // sets and dictionaries merely to recover source lengths.
                for (int first = 0; first < run.Count;)
                {
                    int end = first + 1;
                    bool unsafeBefore = run[first].UnsafeBefore;
                    while (end < run.Count && run[end].Cluster == run[first].Cluster)
                    { unsafeBefore |= run[end].UnsafeBefore; end++; }
                    int length = (end < run.Count ? run[end].Cluster : text.Length) - run[first].Cluster;
                    for (int i = first; i < end; i++)
                        Output(face, output, i, run[i], adjustments[i], length, unsafeBefore);
                    first = end;
                }
            }
            else
            {
                var starts = new SortedSet<int>();
                var unsafeClusters = new HashSet<int>();
                foreach (var glyph in run)
                {
                    starts.Add(glyph.Cluster);
                    if (glyph.UnsafeBefore) unsafeClusters.Add(glyph.Cluster);
                }
                var lengths = new Dictionary<int, int>();
                int previous = -1;
                foreach (int start in starts)
                {
                    if (previous >= 0) lengths.Add(previous, start - previous);
                    previous = start;
                }
                if (previous >= 0) lengths.Add(previous, text.Length - previous);
                for (int i = 0; i < run.Count; i++)
                    Output(face, output, i, run[i], adjustments[i], lengths[run[i].Cluster], unsafeClusters.Contains(run[i].Cluster));
            }
            return new ShapedRun(text, output);
        }

        private static void Output(FontFace face, GlyphRun output, int i, ShapingGlyph glyph, GlyphAdjustment adjustment, int length, bool unsafeBefore)
        {
            if (glyph.Id == 0) throw new NotSupportedException("A glyph remained unmapped after substitution.");
            output.Set(i, glyph.Id, glyph.Cluster,
                checked(face.Font.AdvanceWidth(glyph.Id) + adjustment.XAdvance), adjustment.YAdvance,
                adjustment.XPlacement, adjustment.YPlacement, glyph.ComponentCount,
                adjustment.ZeroMarkAdvance, glyph.Ligature, length, unsafeBefore, glyph.MarkComponent);
        }

        internal static void AppendCluster(FontFace face, List<int> scalars, ScalarCluster cluster,
            int sourceOffset, List<ShapingGlyph> run, List<SourceScalar> tokens = null, bool hebrew = false)
        {
            for (int i = cluster.Start; i < cluster.End; i++)
            {
                int scalar = scalars[i];
                bool consumed;
                if ((scalar == 0xfe0e || scalar == 0xfe0f) && i > cluster.Start)
                {
                    face.MapVariation(scalars[i - 1], scalar, out consumed);
                    if (consumed)
                        continue;
                }
                int id = i + 1 < cluster.End && (scalars[i + 1] == 0xfe0e || scalars[i + 1] == 0xfe0f)
                    ? face.MapVariation(scalar, scalars[i + 1]) : face.Font.GlyphIndex(scalar);
                int? spaceWidth = id == 0 ? SpaceFallback.Width(face.Font, scalar) : null;
                if (spaceWidth.HasValue) id = face.Font.GlyphIndex(32);
                if (id == 0 && !EmojiClusters.Ignorable(scalar))
                    throw new NotSupportedException("Font has no glyph for U+" + scalar.ToString("X4") + ".");
                bool ownControl = hebrew && UnicodeData.IsIgnorable(scalar) && scalar != 0x200d && scalar != 0x34f && scalar != 0xfe0e && scalar != 0xfe0f;
                var glyph = face.RentGlyph(id, scalar, ownControl ? tokens[i].Start : sourceOffset);
                glyph.AdvanceOverride = spaceWidth;
                glyph.Mirrored = tokens != null && scalar != tokens[i].Scalar;
                run.Add(glyph);
            }
        }

        internal static GlyphAdjustment[] PositionRun(GlyphSubstitution substitution, GlyphPositioning positioning,
            List<ShapingGlyph> run, TrueTypeFont font, bool hebrew = false, bool arabic = false)
        {
            substitution.Apply(run);
            for (int i = run.Count - 1; i >= 0; i--) if (EmojiClusters.Ignorable(run[i].Scalar) && !arabic)
            {
                if (hebrew && i + 1 < run.Count && (i == 0 || run[i - 1].Cluster != run[i].Cluster)) run[i + 1].Cluster = Math.Min(run[i + 1].Cluster, run[i].Cluster);
                else if (hebrew && i > 0 && i + 1 == run.Count) run[i - 1].Cluster = Math.Min(run[i - 1].Cluster, run[i].Cluster);
                run.RemoveAt(i);
            }
            var adjustments = positioning.Apply(run, font.AdvanceWidthOf, font.Kerning);
            FallbackMarkPositioning.Apply(font, run, adjustments, positioning.Limits, hebrew);
            if (arabic)
            {
                var positions = new List<GlyphAdjustment>(adjustments);
                for (int i = run.Count - 1; i >= 0; i--) if (UnicodeData.IsIgnorable(run[i].Scalar))
                {
                    int target = i + 1 < run.Count && (i == 0 || run[i - 1].Cluster != run[i].Cluster) ? i + 1 : i > 0 && i + 1 == run.Count ? i - 1 : -1;
                    if (target >= 0 && run[i].Cluster < run[target].Cluster) { run[target].Cluster = run[i].Cluster; run[target].UnsafeBefore = run[i].UnsafeBefore; }
                    run.RemoveAt(i); positions.RemoveAt(i);
                }
                adjustments = positions.ToArray();
            }
            return adjustments;
        }
    }
}
