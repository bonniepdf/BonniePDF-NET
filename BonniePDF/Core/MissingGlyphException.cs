using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BonniePDF;

/// <summary>
/// Raised when text uses a character that no registered font can draw and the document's
/// <see cref="DocumentSettings.MissingGlyphs"/> is <see cref="MissingGlyphs.Throw"/>. With the default,
/// <see cref="MissingGlyphs.Drop"/>, the character is left out and a <c>Font</c> warning is recorded instead.
/// </summary>
public sealed class MissingGlyphException : BonniePdfException
{
    /// <summary>The code points no font could draw, up to eight, in the order they were met.</summary>
    public IReadOnlyList<int> CodePoints { get; }

    /// <summary>The font the text asked for.</summary>
    public string FontName { get; }

    /// <summary>Creates a new <see cref="MissingGlyphException"/>.</summary>
    public MissingGlyphException(IReadOnlyList<int> codePoints, string fontName)
        : base(
            $"No registered font can draw {string.Join(", ", (codePoints ?? throw new ArgumentNullException(nameof(codePoints))).Select(FormatCodePoint))} in text set in {fontName}. " +
            "Register a font that covers these characters, or set Settings.MissingGlyphs to MissingGlyphs.Drop to leave them out.")
    {
        CodePoints = codePoints;
        FontName = fontName;
    }

    /// <summary><c>U+XXXX</c>, at least four hexadecimal digits.</summary>
    internal static string FormatCodePoint(int codePoint) => "U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture);
}
