using BonniePDF.Core;
using BonniePDF.Typography;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Defines the visual style for text rendering. Properties set to null inherit
/// from the parent RichText element's DefaultStyle.
/// </summary>
public class TextStyle
{
    /// <summary>
    /// Font family name (e.g. "Helvetica") or TTF file path.
    /// When used with FontWeight/Italic, the FontFamilyResolver maps
    /// to the correct variant (e.g. "Helvetica-Bold").
    /// Null = inherit from parent.
    /// </summary>
    public string FontFamily { get; set; }

    /// <summary>
    /// Font weight (100-900). Normal = 400, Bold = 700.
    /// </summary>
    public FontWeight Weight { get; set; } = FontWeight.Normal;

    /// <summary>
    /// Whether the text should be rendered in italic/oblique style.
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// Font size in points. Null = inherit from parent.
    /// </summary>
    public float? FontSize { get; set; }

    /// <summary>
    /// Text color. Null = inherit from parent.
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Background highlight color behind the text. Null = no highlight.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// Text decoration (underline, strikethrough, or both).
    /// </summary>
    public TextDecoration Decoration { get; set; } = TextDecoration.None;

    /// <summary>
    /// Resolves the actual PDF font name by combining FontFamily + Weight + Italic
    /// through the FontFamilyResolver.
    /// </summary>
    public string ResolvedFontName =>
        FontFamily != null
            ? FontFamilyResolver.Resolve(FontFamily, Weight, Italic)
            : null;

    /// <summary>
    /// Creates a merged style where this style's non-null values override the base style's values.
    /// Used to combine a span's local style with the RichText element's default style.
    /// </summary>
    public TextStyle MergeWith(TextStyle baseStyle)
    {
        if (baseStyle == null) return this;

        return new TextStyle
        {
            FontFamily = FontFamily ?? baseStyle.FontFamily,
            Weight = Weight != FontWeight.Normal || FontFamily != null ? Weight : baseStyle.Weight,
            Italic = Italic || (FontFamily == null && baseStyle.Italic),
            FontSize = FontSize ?? baseStyle.FontSize,
            Color = Color ?? baseStyle.Color,
            BackgroundColor = BackgroundColor ?? baseStyle.BackgroundColor,
            Decoration = Decoration != TextDecoration.None ? Decoration : baseStyle.Decoration
        };
    }

    /// <summary>
    /// Returns the effective font size, defaulting to 12 if not set.
    /// </summary>
    public float EffectiveFontSize => FontSize ?? 12f;

    /// <summary>
    /// Returns the effective text color, defaulting to black if not set.
    /// </summary>
    public Color EffectiveColor => Color ?? Core.Color.Black;

    /// <summary>
    /// Returns the effective font name, resolving family+weight+italic or defaulting to Helvetica.
    /// </summary>
    public string EffectiveFontName => ResolvedFontName ?? StandardFonts.Helvetica;

    // --- Fluent API ---

    /// <summary>Sets font weight to Bold (700)</summary>
    public TextStyle Bold()
    {
        Weight = FontWeight.Bold;
        return this;
    }

    /// <summary>Sets font weight to Light (300)</summary>
    public TextStyle Light()
    {
        Weight = FontWeight.Light;
        return this;
    }

    /// <summary>Sets font weight to Thin (100)</summary>
    public TextStyle Thin()
    {
        Weight = FontWeight.Thin;
        return this;
    }

    /// <summary>Sets italic to true</summary>
    public TextStyle WithItalic()
    {
        Italic = true;
        return this;
    }

    /// <summary>Sets the font size</summary>
    public TextStyle WithSize(float size)
    {
        FontSize = size;
        return this;
    }

    /// <summary>Sets the text color</summary>
    public TextStyle WithColor(Color color)
    {
        Color = color;
        return this;
    }

    /// <summary>Sets the background highlight color</summary>
    public TextStyle WithBackground(Color color)
    {
        BackgroundColor = color;
        return this;
    }

    /// <summary>Sets the font weight</summary>
    public TextStyle WithWeight(FontWeight weight)
    {
        Weight = weight;
        return this;
    }

    /// <summary>Sets the font family</summary>
    public TextStyle WithFamily(string family)
    {
        FontFamily = family;
        return this;
    }

    /// <summary>Adds underline decoration</summary>
    public TextStyle Underline()
    {
        Decoration |= TextDecoration.Underline;
        return this;
    }

    /// <summary>Adds strikethrough decoration</summary>
    public TextStyle Strikethrough()
    {
        Decoration |= TextDecoration.Strikethrough;
        return this;
    }
}
