using BonniePDF.Core;
using BonniePDF.Elements.Text;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for text blocks
/// </summary>
public static class TextBlockExtensions
{
    /// <summary>
    /// Sets the text alignment to left
    /// </summary>
    public static TextBlock AlignLeft(this TextBlock b)
    {
        b.Alignment = TextAlignment.Left;
        return b;
    }

    /// <summary>
    /// Sets the text alignment to center
    /// </summary>
    public static TextBlock AlignCenter(this TextBlock b)
    {
        b.Alignment = TextAlignment.Center;
        return b;
    }

    /// <summary>
    /// Sets the text alignment to right
    /// </summary>
    public static TextBlock AlignRight(this TextBlock b)
    {
        b.Alignment = TextAlignment.Right;
        return b;
    }

    /// <summary>
    /// Sets the text alignment to justified
    /// </summary>
    public static TextBlock AlignJustify(this TextBlock b)
    {
        b.Alignment = TextAlignment.Justify;
        return b;
    }

    /// <summary>
    /// Adds underline decoration to the text block
    /// </summary>
    public static TextBlock Underline(this TextBlock b)
    {
        b.TextDecoration |= TextDecoration.Underline;
        return b;
    }

    /// <summary>
    /// Adds strikethrough decoration to the text block
    /// </summary>
    public static TextBlock Strikethrough(this TextBlock b)
    {
        b.TextDecoration |= TextDecoration.Strikethrough;
        return b;
    }

    /// <summary>
    /// Sets the font for the text block
    /// </summary>
    public static TextBlock Font(this TextBlock b, string fontName, float? fontSize = null)
    {
        b.FontName = fontName;
        b.FontSize = fontSize ?? b.FontSize;
        return b;
    }

    /// <summary>
    /// Sets the font size for the text block
    /// </summary>
    public static TextBlock FontSize(this TextBlock b, float size)
    {
        b.FontSize = size;
        return b;
    }

    /// <summary>
    /// Sets the text color for the text block
    /// </summary>
    public static TextBlock TextColor(this TextBlock b, Color color)
    {
        b.TextColor = color;
        return b;
    }
}
