namespace BonniePDF;

/// <summary>
/// Horizontal alignment of text within the width available to it.
/// </summary>
public enum TextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2,
    Middle = 3,
    Justify = 4,
    /// <summary>Inline start: left in LTR, right in RTL.</summary>
    Start = 5,
    /// <summary>Inline end: right in LTR, left in RTL.</summary>
    End = 6
}