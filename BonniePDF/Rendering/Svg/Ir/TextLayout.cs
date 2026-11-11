// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BonniePDF.Rendering.Svg.Fonts;
using BonniePDF.Rendering.Svg.Parsers;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>Consecutive glyphs sharing one style, as user-space path segments.</summary>
/// <param name="Style">The style the glyphs are painted in.</param>
/// <param name="Segments">The glyph outlines.</param>
/// <param name="Source">The element the characters came from, for diagnostics only.</param>
internal sealed record TextRun(ComputedStyle Style, List<PathSegment> Segments, string Source);

/// <summary>Text layout: <c>text</c> and <c>tspan</c> to glyph outlines.</summary>
/// <remarks>
/// <para>
/// Text is imported as outlines, so everything a renderer would do to place glyphs
/// happens here - whitespace handling, x/y/dx/dy/rotate lists, text chunks and their
/// anchoring, <c>textLength</c>, and choosing a font per character. What comes out is
/// ordinary path geometry, one run per stretch of text that shares a style, which
/// the builder turns into paths like any shape.
/// </para>
/// <para>
/// The only shaping it does is pair kerning, which both reference renderers apply
/// and Latin text visibly needs. No ligatures, no contextual forms: scripts that
/// cannot be drawn correctly without them are reported rather than drawn wrongly.
/// </para>
/// <para>
/// Mirrors ts/src/Ir/Text.ts. Runs are split on style <em>identity</em>, compared
/// with <see cref="object.ReferenceEquals"/>: <see cref="ComputedStyle"/> is a record,
/// so <c>==</c> is value equality, and a tspan that changes nothing would merge into
/// its neighbours here while staying separate in TypeScript.
/// </para>
/// </remarks>
internal static partial class TextLayout
{
    private const int Space = 0x20;

    private sealed record Holder(SvgElement Element, ComputedStyle Style);

    private sealed class Char(int codePoint, ComputedStyle style, int[] holders, string source)
    {
        public int CodePoint { get; set; } = codePoint;
        public ComputedStyle Style { get; } = style;
        public int[] Holders { get; } = holders;
        public string Source { get; } = source;
        public double? X { get; set; }
        public double? Y { get; set; }
        public double Dx { get; set; }
        public double Dy { get; set; }
        public double Rotate { get; set; }
    }

    private sealed class Placed(IFontFace face, int glyph, double x, double y, double scale, double rotate, ComputedStyle style, string source)
    {
        public IFontFace Face { get; } = face;
        public int Glyph { get; } = glyph;
        public double X { get; set; } = x;
        public double Y { get; } = y;
        public double Scale { get; } = scale;
        public double Stretch { get; set; } = 1;
        public double Rotate { get; } = rotate;
        public ComputedStyle Style { get; } = style;
        public string Source { get; } = source;
    }

    private sealed class Chunk(TextAnchor anchor, double start)
    {
        public List<Placed> Glyphs { get; } = [];
        public TextAnchor Anchor { get; } = anchor;
        public double StartX { get; set; } = start;
        public double EndX { get; set; } = start;
    }

    /// <summary>Problems in first-reported order, a later report of the same feature replacing its detail.</summary>
    private sealed class Problems
    {
        private readonly List<UnsupportedFeature> _entries = [];

        public void Set(string feature, string detail)
        {
            int at = _entries.FindIndex(entry => string.Equals(entry.Feature, feature, StringComparison.Ordinal));
            if (at == -1) _entries.Add(new UnsupportedFeature(feature, detail));
            else _entries[at] = new UnsupportedFeature(feature, detail);
        }

        public IEnumerable<UnsupportedFeature> Entries => _entries;
    }

    /// <summary>Lays out one <c>text</c> element and returns its glyphs as runs.</summary>
    /// <remarks>
    /// Problems are added to <paramref name="unsupported"/> once per text element, so a
    /// chart with a thousand labels in an uncovered script says so once, not a
    /// thousand times.
    /// </remarks>
    public static List<TextRun> Layout(
        SvgElement element,
        ComputedStyle style,
        IFontProvider fonts,
        Func<ComputedStyle, IReadOnlyDictionary<string, string>, ComputedStyle> cascade,
        List<UnsupportedFeature> unsupported)
    {
        var problems = new Problems();
        var holders = new List<Holder> { new(element, style) };
        var raw = new List<Char>();
        Collect(element, style, [0], holders, raw, problems, cascade);

        List<Char> chars = NormaliseSpace(raw);
        AssignPositions(chars, holders);

        List<Chunk> chunks = Place(chars, fonts, problems);
        ApplyTextLength(element, chunks, problems);
        Anchor(chunks);

        unsupported.AddRange(problems.Entries);
        return Runs(chunks);
    }

    /// <summary>Walks the content in document order, cascading style through each tspan.</summary>
    private static void Collect(
        SvgElement element,
        ComputedStyle style,
        int[] chain,
        List<Holder> holders,
        List<Char> chars,
        Problems problems,
        Func<ComputedStyle, IReadOnlyDictionary<string, string>, ComputedStyle> cascade)
    {
        foreach (SvgContent entry in element.Content)
        {
            if (entry.Text is { } text)
            {
                foreach (int codePoint in CodePoints(text))
                {
                    chars.Add(new Char(codePoint, style, chain, element.Name));
                }
                continue;
            }

            SvgElement child = entry.Element!;
            string name = child.Name;
            if (name is "tspan" or "a")
            {
                ComputedStyle childStyle = cascade(style, child.Attributes);
                if (!childStyle.Display) continue;
                if (string.Equals(name, "tspan", StringComparison.Ordinal) && child.Attributes.ContainsKey("textLength"))
                {
                    problems.Set("textLength", "textLength is applied on <text> only, not on <tspan>");
                }
                holders.Add(new Holder(child, childStyle));
                Collect(child, childStyle, [.. chain, holders.Count - 1], holders, chars, problems, cascade);
            }
            else if (string.Equals(name, "textPath", StringComparison.Ordinal))
            {
                problems.Set("textPath", "text on a path is not in the supported profile");
            }
            else if (string.Equals(name, "tref", StringComparison.Ordinal))
            {
                problems.Set("tref", "tref was removed from SVG 2 and is not supported");
            }
            // title, desc and anything unknown inside text contribute no characters.
        }
    }

    /// <summary>
    /// Code points as JavaScript's string iterator yields them: a well-formed
    /// surrogate pair is one code point, and a lone surrogate is its own code unit.
    /// </summary>
    /// <remarks>
    /// Not <see cref="string.EnumerateRunes"/>, which replaces a lone surrogate with
    /// U+FFFD - the two engines must see the same characters for every input,
    /// malformed or not.
    /// </remarks>
    private static IEnumerable<int> CodePoints(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                yield return char.ConvertToUtf32(c, text[i + 1]);
                i++;
            }
            else
            {
                yield return c;
            }
        }
    }

    /// <summary>
    /// Whitespace, as browsers treat it: newlines and tabs become spaces, and outside
    /// <c>xml:space="preserve"</c> a run of spaces collapses to one and spaces at the
    /// start and end of the element go. SVG 1.1 removed newlines outright; SVG 2 and
    /// every browser convert them, so "a\nb" reads "a b".
    /// </summary>
    private static List<Char> NormaliseSpace(List<Char> raw)
    {
        var output = new List<Char>(raw.Count);
        foreach (Char character in raw)
        {
            if (character.CodePoint is 0x0A or 0x0D or 0x09) character.CodePoint = Space;

            if (!character.Style.PreserveSpace && character.CodePoint == Space)
            {
                Char? previous = output.Count > 0 ? output[^1] : null;
                if (previous is null || (previous.CodePoint == Space && !previous.Style.PreserveSpace)) continue;
            }
            output.Add(character);
        }

        while (output.Count > 0)
        {
            Char last = output[^1];
            if (last.CodePoint != Space || last.Style.PreserveSpace) break;
            output.RemoveAt(output.Count - 1);
        }
        return output;
    }

    /// <summary>
    /// Applies x, y, dx, dy and rotate lists. Each list addresses the characters of the
    /// element carrying it, from its first; holders are visited outermost first, so a
    /// tspan's own list overrides its text's for the characters it holds. A rotate
    /// list's last value carries on to the remaining characters; the others simply run
    /// out.
    /// </summary>
    private static void AssignPositions(List<Char> chars, List<Holder> holders)
    {
        for (int index = 0; index < holders.Count; index++)
        {
            (SvgElement element, ComputedStyle style) = holders[index];
            int holder = index;
            List<Char> members = chars.Where(character => character.Holders.Contains(holder)).ToList();
            if (members.Count == 0) continue;

            List<double> List(string name) => LengthList(element.Attributes.GetValueOrDefault(name), style.FontSize);

            List<double> xs = List("x");
            for (int i = 0; i < xs.Count && i < members.Count; i++) members[i].X = xs[i];
            List<double> ys = List("y");
            for (int i = 0; i < ys.Count && i < members.Count; i++) members[i].Y = ys[i];
            List<double> dxs = List("dx");
            for (int i = 0; i < dxs.Count && i < members.Count; i++) members[i].Dx = dxs[i];
            List<double> dys = List("dy");
            for (int i = 0; i < dys.Count && i < members.Count; i++) members[i].Dy = dys[i];

            List<double> rotate = List("rotate");
            if (rotate.Count > 0)
            {
                for (int i = 0; i < members.Count; i++) members[i].Rotate = rotate[Math.Min(i, rotate.Count - 1)];
            }
        }
    }

    private static List<double> LengthList(string? value, double fontSize)
    {
        var values = new List<double>();
        if (value is null) return values;
        // Explicit separators rather than \s, whose Unicode coverage differs between
        // JavaScript and .NET - the two engines must split every list identically.
        foreach (string token in ListSeparators().Split(value))
        {
            if (token.Length == 0) continue;
            if (LengthParser.Parse(token, new LengthContext(0, fontSize)) is not { } parsed) return values;
            values.Add(parsed);
        }
        return values;
    }

    [GeneratedRegex("[ \t\n\r\f,]+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ListSeparators();

    /// <summary>
    /// Chooses a glyph for every character, advances the pen, and splits the result
    /// into text chunks - a chunk starts wherever a character is given an absolute x or
    /// y, and is anchored as a unit.
    /// </summary>
    private static List<Chunk> Place(List<Char> chars, IFontProvider fonts, Problems problems)
    {
        var chunks = new List<Chunk>();
        var faceLists = new Dictionary<string, List<IFontFace>>(StringComparer.Ordinal);
        var uncovered = new List<int>();
        var missingFamilies = new List<string>();
        bool complexScript = false;

        double penX = 0;
        double penY = 0;
        Chunk? chunk = null;
        Placed? previous = null;

        foreach (Char character in chars)
        {
            bool absolute = character.X is not null || character.Y is not null;
            if (character.X is { } x) penX = x;
            if (character.Y is { } y) penY = y;
            if (chunk is null || absolute)
            {
                chunk = new Chunk(character.Style.TextAnchor, penX);
                chunks.Add(chunk);
                previous = null;
            }
            penX += character.Dx;
            penY += character.Dy;

            foreach (string issue in character.Style.TextIssues)
            {
                problems.Set(issue, $"{issue} is not applied by the text layout");
            }
            if (NeedsShaping(character.CodePoint)) complexScript = true;

            List<IFontFace> faces = FacesFor(character.Style, fonts, faceLists);
            if (faces.Count == 0)
            {
                if (!missingFamilies.Contains(character.Style.FontFamily)) missingFamilies.Add(character.Style.FontFamily);
                previous = null;
                continue;
            }

            IFontFace? face = null;
            int glyph = 0;
            foreach (IFontFace candidate in faces)
            {
                glyph = candidate.GlyphIndex(character.CodePoint);
                if (glyph != 0)
                {
                    face = candidate;
                    break;
                }
            }
            // A character no supplied font covers is dropped and reported, never drawn
            // as the font's .notdef box - the same rule BonniePDF applies to its own text.
            if (face is null)
            {
                if (!uncovered.Contains(character.CodePoint)) uncovered.Add(character.CodePoint);
                previous = null;
                continue;
            }

            double scale = character.Style.FontSize / face.UnitsPerEm;
            // Kerning adjusts the previous glyph's advance, so it applies only between
            // neighbours in one face at one size: across a change of font the pair is
            // not a pair in any font's kerning table.
            if (previous is not null && ReferenceEquals(previous.Face, face) && previous.Scale == scale)
            {
                penX += face.Kerning(previous.Glyph, glyph) * scale;
            }
            if (chunk.Glyphs.Count == 0) chunk.StartX = penX;
            var placed = new Placed(face, glyph, penX, penY, scale, character.Rotate, character.Style, character.Source);
            chunk.Glyphs.Add(placed);
            previous = placed;
            penX +=
                face.AdvanceWidth(glyph) * scale +
                character.Style.LetterSpacing +
                (character.CodePoint == Space ? character.Style.WordSpacing : 0);
            chunk.EndX = penX;
        }

        var fontProblems = new List<string>();
        if (missingFamilies.Count > 0)
        {
            fontProblems.Add($"no supplied font matches font-family: {string.Join(" | ", missingFamilies)}");
        }
        if (uncovered.Count > 0)
        {
            IEnumerable<string> listed = uncovered.Take(8).Select(codePoint => $"U+{codePoint:X4}");
            string more = uncovered.Count > 8 ? $" and {uncovered.Count - 8} more" : string.Empty;
            fontProblems.Add($"no supplied font covers {string.Join(", ", listed)}{more}");
        }
        if (fontProblems.Count > 0) problems.Set("font", string.Join("; ", fontProblems));
        if (complexScript)
        {
            problems.Set("complex script", "text in a script that needs shaping (Arabic, Hebrew, Indic, ...) is not laid out correctly without it");
        }
        return chunks.Where(entry => entry.Glyphs.Count > 0).ToList();
    }

    /// <summary>The faces a style's font-family list resolves to, in list order, without repeats.</summary>
    private static List<IFontFace> FacesFor(ComputedStyle style, IFontProvider fonts, Dictionary<string, List<IFontFace>> cache)
    {
        // The key spells the weight and italic flag the way JavaScript would, though
        // only equality matters here.
        string key = $"{style.FontFamily}\0{style.FontWeight.ToString(CultureInfo.InvariantCulture)}\0{style.FontItalic}";
        if (!cache.TryGetValue(key, out List<IFontFace>? faces))
        {
            faces = [];
            foreach (string family in FamilyList(style.FontFamily))
            {
                // Rounded as JavaScript's Math.round does, so both engines ask for the same face.
                IFontFace? face = fonts.Resolve(family, (int)Math.Floor(style.FontWeight + 0.5), style.FontItalic);
                if (face is not null && !faces.Contains(face)) faces.Add(face);
            }
            cache[key] = faces;
        }
        return faces;
    }

    /// <summary>A CSS font-family list: comma-separated, names optionally quoted.</summary>
    public static List<string> FamilyList(string value)
    {
        var families = new List<string>();
        foreach (string part in value.Split(','))
        {
            string name = QuotedName().Replace(part.Trim(), "${name}").Trim();
            if (name.Length > 0) families.Add(name);
        }
        return families;
    }

    [GeneratedRegex("^(?<quote>['\"])(?<name>.*)\\k<quote>$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex QuotedName();

    /// <summary>
    /// Code points in scripts whose glyphs must be joined, reordered or combined by a
    /// shaping engine to read correctly. Drawing them one glyph at a time produces text
    /// that looks plausible to someone who cannot read it, which is the worst kind of
    /// wrong.
    /// </summary>
    private static bool NeedsShaping(int codePoint) =>
        codePoint is (>= 0x0590 and <= 0x08FF) // Hebrew, Arabic, Syriac, Thaana, NKo, ...
            or (>= 0x0900 and <= 0x0DFF) // Indic
            or (>= 0x0E00 and <= 0x0EFF) // Thai, Lao
            or (>= 0x0F00 and <= 0x0FFF) // Tibetan
            or (>= 0x1000 and <= 0x109F) // Myanmar
            or (>= 0x1780 and <= 0x17FF) // Khmer
            or (>= 0xFB1D and <= 0xFDFF) // Hebrew and Arabic presentation forms
            or (>= 0xFE70 and <= 0xFEFF); // Arabic presentation forms B

    /// <summary>
    /// <c>textLength</c> on the text element. <c>spacing</c> (the default) spreads the
    /// difference over the gaps between glyphs; <c>spacingAndGlyphs</c> stretches glyphs
    /// and gaps alike. Applied before anchoring, so the anchor sees the stated length.
    /// </summary>
    private static void ApplyTextLength(SvgElement element, List<Chunk> chunks, Problems problems)
    {
        if (!element.Attributes.TryGetValue("textLength", out string? value) || chunks.Count == 0) return;
        if (LengthParser.Parse(value, new LengthContext(0, 16)) is not { } target || target < 0) return;
        if (chunks.Count > 1)
        {
            problems.Set("textLength", "textLength across more than one text chunk is not applied");
            return;
        }

        Chunk chunk = chunks[0];
        double natural = chunk.EndX - chunk.StartX;
        if (string.Equals(element.Attributes.GetValueOrDefault("lengthAdjust")?.Trim(), "spacingAndGlyphs", StringComparison.Ordinal))
        {
            double factor = natural > 0 ? target / natural : 1;
            foreach (Placed glyph in chunk.Glyphs)
            {
                glyph.X = chunk.StartX + (glyph.X - chunk.StartX) * factor;
                glyph.Stretch = factor;
            }
        }
        else
        {
            int gaps = chunk.Glyphs.Count - 1;
            if (gaps > 0)
            {
                double extra = (target - natural) / gaps;
                for (int i = 0; i < chunk.Glyphs.Count; i++) chunk.Glyphs[i].X += extra * i;
            }
        }
        chunk.EndX = chunk.StartX + target;
    }

    private static void Anchor(List<Chunk> chunks)
    {
        foreach (Chunk chunk in chunks)
        {
            if (chunk.Anchor == TextAnchor.Start) continue;
            double width = chunk.EndX - chunk.StartX;
            double shift = chunk.Anchor == TextAnchor.Middle ? -width / 2 : -width;
            foreach (Placed glyph in chunk.Glyphs) glyph.X += shift;
        }
    }

    /// <summary>Glyph outlines in user space, grouped into runs of one style.</summary>
    private static List<TextRun> Runs(List<Chunk> chunks)
    {
        var result = new List<TextRun>();
        TextRun? current = null;

        foreach (Chunk chunk in chunks)
        {
            foreach (Placed glyph in chunk.Glyphs)
            {
                List<PathSegment> segments = GlyphSegments(glyph);
                if (segments.Count == 0) continue;
                if (current is null || !ReferenceEquals(current.Style, glyph.Style))
                {
                    current = new TextRun(glyph.Style, [], glyph.Source);
                    result.Add(current);
                }
                current.Segments.AddRange(segments);
            }
        }
        return result;
    }

    /// <summary>
    /// One glyph's outline, placed. Font units are y-up and user space y-down, so y is
    /// negated; a glyph rotation turns about its own origin on the baseline. Quadratic
    /// segments are raised to cubic after placing - an affine map takes a Bezier to a
    /// Bezier of the same degree, so the order does not change the curve. Cubic
    /// segments, from CFF outlines, are placed as they are.
    /// </summary>
    private static List<PathSegment> GlyphSegments(Placed glyph)
    {
        IReadOnlyList<GlyphSegment> outline = glyph.Face.Outline(glyph.Glyph);
        var segments = new List<PathSegment>(outline.Count);
        if (outline.Count == 0) return segments;

        double radians = glyph.Rotate * Math.PI / 180;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        (double X, double Y) Map(double gx, double gy)
        {
            double lx = gx * glyph.Scale * glyph.Stretch;
            double ly = -gy * glyph.Scale;
            if (glyph.Rotate == 0) return (glyph.X + lx, glyph.Y + ly);
            return (glyph.X + lx * cos - ly * sin, glyph.Y + lx * sin + ly * cos);
        }

        double currentX = 0;
        double currentY = 0;
        double startX = 0;
        double startY = 0;

        foreach (GlyphSegment segment in outline)
        {
            if (segment.Kind == GlyphSegmentKind.Close)
            {
                segments.Add(PathSegment.Close());
                currentX = startX;
                currentY = startY;
                continue;
            }

            (double x, double y) = Map(segment.X, segment.Y);
            if (segment.Kind == GlyphSegmentKind.Move)
            {
                segments.Add(PathSegment.Move(x, y));
                startX = x;
                startY = y;
            }
            else if (segment.Kind == GlyphSegmentKind.Line)
            {
                segments.Add(PathSegment.Line(x, y));
            }
            else if (segment.Kind == GlyphSegmentKind.Cubic)
            {
                (double c1x, double c1y) = Map(segment.CX, segment.CY);
                (double c2x, double c2y) = Map(segment.C2X, segment.C2Y);
                segments.Add(new PathSegment(PathSegmentKind.Cubic, x, y, c1x, c1y, c2x, c2y));
            }
            else
            {
                (double qx, double qy) = Map(segment.CX, segment.CY);
                segments.Add(new PathSegment(
                    PathSegmentKind.Cubic,
                    x,
                    y,
                    currentX + 2.0 / 3.0 * (qx - currentX),
                    currentY + 2.0 / 3.0 * (qy - currentY),
                    x + 2.0 / 3.0 * (qx - x),
                    y + 2.0 / 3.0 * (qy - y)));
            }
            currentX = x;
            currentY = y;
        }
        return segments;
    }
}
