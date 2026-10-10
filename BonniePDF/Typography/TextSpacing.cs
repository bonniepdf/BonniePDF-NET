using System.Text;

namespace BonniePDF;

/// <summary>
/// Shared letter/word-spacing advance calculation used by the text elements and
/// the canvas painters, so measurement and painting can never drift apart.
/// </summary>
internal static class TextSpacing
{
    /// <summary>
    /// Extra advance contributed by letter and word spacing for a string: letter
    /// spacing after every glyph plus word spacing after every space. Control
    /// characters below U+0020 are not counted, matching the glyph-positioning
    /// loops that emit the text. Zero when neither spacing is set.
    /// </summary>
    /// <param name="text">The text whose extra advance is measured.</param>
    /// <param name="letterSpacing">Additional advance after every glyph, in points.</param>
    /// <param name="wordSpacing">Additional advance after every space, in points.</param>
    /// <returns>The extra advance in points.</returns>
    public static float GetWidth(string text, float letterSpacing, float wordSpacing, string? fontIdentifier=null, System.Collections.Generic.IReadOnlyDictionary<string,int>? features=null)
    {
        if (letterSpacing == 0 && wordSpacing == 0) return 0;

        int glyphs = 0;
        int spaces = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (rune.Value < 32) continue;
            glyphs++;
            if (rune.Value == ' ') spaces++;
        }

        if(fontIdentifier is not null)glyphs=FontMetrics.GetGlyphCount(text,fontIdentifier,features);
        return glyphs * letterSpacing + spaces * wordSpacing;
    }
}
