using System.Collections.Generic;
using System.Text;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>A maximal piece of a standard-face text that the face shows (<see cref="Covered"/>) or that falls back.</summary>
internal readonly record struct StandardTextSegment(string Text, bool Covered);

/// <summary>
/// Which characters a non-embedded standard text face shows itself, and the faces it falls back to for the rest:
/// its Liberation substitute first, then the document's faces, then the global registrations.
/// </summary>
internal static class StandardFontCoverage
{
    // The twelve standard text faces. Symbol and ZapfDingbats use their own built-in encodings and never fall back.
    private static readonly HashSet<string> TextFaces = new(System.StringComparer.Ordinal)
    {
        "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
        "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
        "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
    };

    /// <summary>Whether <paramref name="fontName"/> is one of the twelve standard text faces, the faces that fall back outside WinAnsi.</summary>
    internal static bool IsStandardTextFace(string fontName) => TextFaces.Contains(fontName);

    /// <summary>
    /// Whether a standard text face shows every character of <paramref name="text"/> itself. The common case, so it
    /// is checked without segmenting: any UTF-16 unit above U+00FF that WinAnsi does not map (a surrogate included)
    /// means no.
    /// </summary>
    internal static bool CoversAll(string text)
    {
        foreach (char unit in text)
            if (unit > 0xFF && !WinAnsi.Covers(unit)) return false;
        return true;
    }

    /// <summary>
    /// Splits a standard-face text into covered and fallback pieces, by grapheme cluster: a cluster is covered when
    /// the face shows every code point in it, so a letter and its combining marks are never split between fonts.
    /// </summary>
    internal static List<StandardTextSegment> Split(string text)
    {
        var segments = new List<StandardTextSegment>();
        var bounds = Typography.Engine.Graphemes.Boundaries(text);
        var current = new StringBuilder();
        bool? currentCovered = null;
        for (int b = 0; b + 1 < bounds.Count; b++)
        {
            string cluster = text.Substring(bounds[b], bounds[b + 1] - bounds[b]);
            bool covered = true;
            foreach (Rune rune in cluster.EnumerateRunes())
            {
                if (!WinAnsi.Covers(rune.Value)) { covered = false; break; }
            }
            if (currentCovered != covered && currentCovered is bool previous)
            {
                segments.Add(new StandardTextSegment(current.ToString(), previous));
                current.Clear();
            }
            current.Append(cluster);
            currentCovered = covered;
        }
        if (currentCovered is bool last) segments.Add(new StandardTextSegment(current.ToString(), last));
        return segments;
    }

    /// <summary>
    /// The faces a standard text face falls back to, in order: its Liberation substitute when registered, then the
    /// document's faces, then the global registrations. A face reached by two routes appears once, at its first place.
    /// </summary>
    internal static List<FallbackFont> FallbackChain(string fontName)
    {
        var chain = new List<FallbackFont>();
        var seen = new HashSet<TtfFont>(ReferenceEqualityComparer.Instance);
        void Add(FallbackFont fallback)
        {
            if (seen.Add(fallback.Font)) chain.Add(fallback);
        }
        if (FontManager.GetStandardSubstitute(fontName) is FallbackFont substitute) Add(substitute);
        foreach (FallbackFont fallback in FontMetrics.ActiveDocumentFonts?.FallbackFonts() ?? []) Add(fallback);
        foreach (FallbackFont fallback in FontManager.GetFallbackFonts()) Add(fallback);
        return chain;
    }
}
