namespace BonniePDF.Typography;

/// <summary>
/// Helpers for deciding which Unicode code points may legitimately become
/// glyphs in a content stream.
/// </summary>
/// <remarks>
/// The encoder must never emit a <c>.notdef</c> (GID 0) reference for a real
/// code point: PDF/A-2 (6.2.11.8) and PDF/UA-1 (7.21.8) both forbid
/// <c>.notdef</c> references from text-showing operators, and PDF/A-2
/// (6.2.11.7.2) / PDF/UA-1 (7.21.7) additionally require every shown code to
/// carry a ToUnicode mapping (which GID 0 never gets). Two classes of code
/// point are handled here:
/// <list type="bullet">
///   <item><description><b>Default-ignorable / zero-width format characters</b>
///     (variation selectors, joiners, BOM, bidi controls …). These are never
///     rendered as standalone glyphs; some fonts even carry an outline for them,
///     which is worse. They are dropped before shaping.</description></item>
///   <item><description><b>Control characters</b> (&lt; U+0020, plus DEL).
///     Already skipped by the legacy paths; folded in here so every emission
///     path agrees.</description></item>
/// </list>
/// </remarks>
public static class TextEncoding
{
    /// <summary>
    /// Code points that carry the Unicode <c>Default_Ignorable_Code_Point</c>
    /// property (the ranges BonniePDF can realistically encounter) plus the
    /// byte-order mark. These must be stripped before glyph selection so they
    /// never reach a text-showing operator.
    /// </summary>
    public static bool IsDefaultIgnorableCodePoint(int cp)
    {
        return cp == 0x00AD || // SOFT HYPHEN
               cp == 0x034F || // COMBINING GRAPHEME JOINER
               (cp >= 0x200B && cp <= 0x200F) || // ZWSP, ZWNJ, ZWJ, LRM, RLM
               (cp >= 0x202A && cp <= 0x202E) || // bidi embedding/override controls
               (cp >= 0x2060 && cp <= 0x2064) || // WORD JOINER … invisible operators
               (cp >= 0x206A && cp <= 0x206F) || // deprecated format controls
               cp == 0xFEFF || // ZERO WIDTH NO-BREAK SPACE / BOM
               (cp >= 0xFE00 && cp <= 0xFE0F) || // variation selectors 1–16
               (cp >= 0xE0100 && cp <= 0xE01EF); // variation selectors 17–256
    }

    /// <summary>
    /// True when a code point may be turned into a glyph at all. Filters control
    /// characters (the renderer positions whitespace itself) and default-ignorable
    /// format characters. Whitespace &gt;= U+0020 (e.g. the regular space) is kept
    /// so word spacing and justification keep working.
    /// </summary>
    public static bool IsRenderableCodePoint(int cp)
    {
        if (cp < 0x20) return false;            // C0 controls (incl. tab/newline handled by layout)
        if (cp == 0x7F) return false;           // DEL
        if (cp >= 0x80 && cp <= 0x9F) return false; // C1 controls
        if (IsDefaultIgnorableCodePoint(cp)) return false;
        return true;
    }
}
