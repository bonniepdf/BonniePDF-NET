// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
namespace BonniePDF.Typography.Engine
{
    internal sealed class ItemizedParagraph
    {
        internal readonly BidiParagraph Bidi;
        internal readonly ReadOnlyCollection<TextItem> Items;
        internal readonly ShapingLimits Limits;
        private readonly ReadOnlyCollection<TextItem> clusters;
        internal ItemizedParagraph(BidiParagraph bidi, List<TextItem> clusters, ShapingLimits limits)
        { Bidi = bidi; Limits = limits; this.clusters = Array.AsReadOnly(clusters.ToArray()); Items = Merge(clusters); }
        private static ReadOnlyCollection<TextItem> Merge(IEnumerable<TextItem> source)
        {
            var output = new List<TextItem>();
            foreach (var c in source)
            {
                var prior = output.Count == 0 ? null : output[output.Count - 1];
                if (prior != null && prior.End == c.Start && prior.Face == c.Face && prior.Script == c.Script && prior.Level == c.Level)
                    output[output.Count - 1] = new TextItem(prior.Start, c.End, prior.Script, prior.Level, prior.Face);
                else output.Add(c);
            }
            return output.AsReadOnly();
        }
        internal ReadOnlyCollection<ShapedTextItem> ShapeLine(int start = 0, int? end = null, ShapeOptions options = null)
        {
            string text = Bidi.Input.Text; int last = end ?? text.Length; options = options ?? new ShapeOptions(); options.ValidateText(text);
            var boundaries = new HashSet<int>(clusters.Select(c => c.Start)); boundaries.Add(text.Length);
            if (!boundaries.Contains(start) || !boundaries.Contains(last) || last < start) throw new ArgumentException("Line range must contain complete grapheme clusters.");
            var line = Bidi.ReorderLine(start, last); int first = Bidi.Input.Offsets.IndexOf(start), scalar = first;
            var selected = new List<TextItem>();
            foreach (var c in clusters) if (c.Start >= start && c.End <= last)
            {
                while (Bidi.Input.Offsets[scalar] < c.Start) scalar++;
                int level = -1, j = scalar;
                while (j < Bidi.Input.Points.Count && Bidi.Input.Offsets[j] < c.End) { if (line.Levels[j - first] >= 0 && level < 0) level = line.Levels[j - first]; j++; }
                scalar = j; if (level >= 0) selected.Add(new TextItem(c.Start, c.End, c.Script, level, c.Face));
            }
            var shaped = new List<ShapedTextItem>(); int count = 0;
            foreach (var item in Merge(selected))
            {
                if (item.Script != "Latn" && item.Script != "Grek" && item.Script != "Cyrl" && item.Script != "Hebr" && item.Script != "Arab" && item.Script != "Zyyy" && item.Script != "Zinh")
                    throw new NotSupportedException("No shaper is available for script " + item.Script + ".");
                var features = new List<ShapeFeature>();
                foreach (var f in options.Features)
                {
                    int a = Math.Max(item.Start, f.Start), b = Math.Min(item.End, f.End);
                    if (a < b) features.Add(new ShapeFeature(f.Tag, f.Value, a - item.Start, b - item.Start));
                }
                var settings = new ShapeOptions(item.Script == "Zyyy" || item.Script == "Zinh" ? "DFLT" : item.Script.ToLowerInvariant(), options.Language,
                    item.Level % 2 != 0 ? TextDirection.RightToLeft : TextDirection.LeftToRight, features, options.Limits ?? Limits);
                var result = Shaper.Shape(item.Face, text.Substring(item.Start, item.End - item.Start), settings, true);
                var packed = result.Run;
                count = checked(count + packed.Count); if (count > Math.Min(Limits.MaxGlyphs, options.Limits == null ? Limits.MaxGlyphs : options.Limits.MaxGlyphs)) throw new InvalidDataException("Paragraph glyph limit exceeded.");
                var glyphs = new GlyphInfo[packed.Count];
                for (int g = 0; g < glyphs.Length; g++)
                    glyphs[g] = new GlyphInfo(packed.Id(g), item.Start + packed.Cluster(g), packed.AdvanceX(g), packed.AdvanceY(g), packed.OffsetX(g), packed.OffsetY(g),
                        packed.ComponentCount(g), packed.IsMark(g), packed.IsLigature(g), packed.SourceLength(g), packed.UnsafeToBreak(g), packed.ComponentIndex(g));
                shaped.Add(new ShapedTextItem(item, glyphs));
            }
            int max = 0, minOdd = 127;
            foreach (var item in shaped) { max = Math.Max(max, item.Item.Level); if (item.Item.Level % 2 != 0) minOdd = Math.Min(minOdd, item.Item.Level); }
            for (int level = max; level >= minOdd; level--) for (int i = 0; i < shaped.Count;)
            {
                if (shaped[i].Item.Level < level) { i++; continue; }
                int stop = i + 1; while (stop < shaped.Count && shaped[stop].Item.Level >= level) stop++;
                shaped.Reverse(i, stop - i); i = stop;
            }
            return shaped.AsReadOnly();
        }
    }
}
