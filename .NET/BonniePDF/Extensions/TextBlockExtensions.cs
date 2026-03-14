using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

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
    /// Sets the font family for the text block. When used with <see cref="FontWeight"/>
    /// or <see cref="Italic"/>, the correct font variant is resolved automatically
    /// (e.g. "Helvetica" + Bold → "Helvetica-Bold").
    /// </summary>
    public static TextBlock Font(this TextBlock b, string fontName, float? fontSize = null)
    {
        b.FontFamily = fontName;
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
    /// Sets the font weight for the text block (e.g. FontWeight.Bold, FontWeight.Light).
    /// Requires FontFamily to be set via <see cref="Font"/> for automatic resolution.
    /// </summary>
    public static TextBlock FontWeight(this TextBlock b, FontWeight weight)
    {
        b.Weight = weight;
        return b;
    }

    /// <summary>
    /// Sets the font weight to Bold (700).
    /// Requires FontFamily to be set via <see cref="Font"/> for automatic resolution.
    /// </summary>
    public static TextBlock Bold(this TextBlock b)
    {
        b.Weight = Typography.FontWeight.Bold;
        return b;
    }

    /// <summary>
    /// Sets the font weight to Light (300).
    /// Requires FontFamily to be set via <see cref="Font"/> for automatic resolution.
    /// </summary>
    public static TextBlock Light(this TextBlock b)
    {
        b.Weight = Typography.FontWeight.Light;
        return b;
    }

    /// <summary>
    /// Enables italic/oblique rendering.
    /// Requires FontFamily to be set via <see cref="Font"/> for automatic resolution.
    /// </summary>
    public static TextBlock Italic(this TextBlock b)
    {
        b.IsItalic = true;
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
