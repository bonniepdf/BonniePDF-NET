using System;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Text decoration styles that can be combined using bitwise OR.
/// </summary>
[Flags]
public enum TextDecoration
{
    /// <summary>No decoration</summary>
    None = 0,

    /// <summary>Underline drawn below the text baseline</summary>
    Underline = 1,

    /// <summary>Line drawn through the middle of the text</summary>
    Strikethrough = 2,

    /// <summary>Line drawn above the text (over the cap height)</summary>
    Overline = 4
}
