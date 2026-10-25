namespace BonniePDF;

/// <summary>What happens to a character that no registered font can draw.</summary>
public enum MissingGlyphs
{
    /// <summary>Leave the character out and record a <c>Font</c> warning (the default).</summary>
    Drop,
    /// <summary>Stop generation with a <see cref="MissingGlyphException"/>.</summary>
    Throw
}
