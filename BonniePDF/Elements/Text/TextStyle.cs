
namespace BonniePDF;

/// <summary>
/// Defines the visual style for text rendering. Properties set to null inherit
/// from the parent RichText element's DefaultStyle.
/// </summary>
public class TextStyle
{
    private System.Collections.Generic.IReadOnlyDictionary<string,int>? _fontFeatures;
    /// <summary>OpenType feature overrides, such as kern=0, liga=0 or smcp=1. Null inherits font defaults.</summary>
    public System.Collections.Generic.IReadOnlyDictionary<string,int>? FontFeatures
    {
        get => _fontFeatures;
        set
        {
            if(value is null) { _fontFeatures=null; return; }
            if(value.Count>256) throw new System.ArgumentOutOfRangeException(nameof(value), "At most 256 font features are supported.");
            var copy=new System.Collections.Generic.Dictionary<string,int>(System.StringComparer.Ordinal);
            foreach(var feature in value) { _ = new Typography.Engine.ShapeFeature(feature.Key,feature.Value); copy.Add(feature.Key,feature.Value); }
            _fontFeatures=new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(copy);
        }
    }

    /// <summary>
    /// Font family name (e.g. "Helvetica") or TTF file path.
    /// When used with FontWeight/Italic, the FontFamilyResolver maps
    /// to the correct variant (e.g. "Helvetica-Bold").
    /// Null = inherit from parent.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Font weight (100-900). Normal = 400, Bold = 700.
    /// </summary>
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;

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
    /// Text decoration (underline, strikethrough, overline, or a combination).
    /// </summary>
    public TextDecoration Decoration { get; set; } = TextDecoration.None;

    /// <summary>
    /// Colour of the decoration lines (underline/strikethrough/overline).
    /// Null = use the text <see cref="Color"/>, which is the default.
    /// </summary>
    public Color? DecorationColor { get; set; }

    /// <summary>
    /// Thickness of the decoration lines in points. Null = the default derived
    /// from font size (<c>max(0.5, fontSize / 18)</c>).
    /// </summary>
    public float? DecorationThickness { get; set; }

    /// <summary>
    /// Semantic vertical alignment (super/subscript). See <see cref="BaselineAlignment"/>.
    /// The shift magnitude derives from <see cref="EffectiveFontSize"/> unless
    /// <see cref="BaselineShift"/> is set explicitly.
    /// </summary>
    public BaselineAlignment BaselineAlignment { get; set; } = BaselineAlignment.Baseline;

    /// <summary>
    /// Explicit baseline shift in points (positive = up). Overrides the magnitude
    /// implied by <see cref="BaselineAlignment"/>. Null = derive from the alignment.
    /// </summary>
    public float? BaselineShift { get; set; }

    /// <summary>
    /// Extra spacing in points added after every glyph (tracking). Null = 0.
    /// Changes advances, so it flows through measurement, PDF (<c>Tc</c>), and SVG.
    /// </summary>
    public float? LetterSpacing { get; set; }

    /// <summary>
    /// Extra spacing in points added after every space (word spacing). Null = 0.
    /// Changes advances, so it flows through measurement, PDF, and SVG.
    /// </summary>
    public float? WordSpacing { get; set; }

    /// <summary>Line style for decorations (solid/wavy/dashed/dotted). Default solid.</summary>
    public DecorationStyle DecorationStyle { get; set; } = DecorationStyle.Solid;

    /// <summary>
    /// Line-height multiplier applied to font size. Null = inherit (1.2 if not set anywhere).
    /// Set to 1.5 for looser leading or 1.0 for tight leading.
    /// </summary>
    public float? LineHeight { get; set; }

    /// <summary>
    /// Resolves the actual PDF font name by combining FontFamily + Weight + Italic
    /// through the FontFamilyResolver.
    /// </summary>
    public string? ResolvedFontName =>
        FontFamily != null
            ? FontFamilyResolver.Resolve(FontFamily, FontWeight, Italic)
            : null;

    /// <summary>
    /// Creates a merged style where this style's non-null values override the base style's values.
    /// Used to combine a span's local style with the RichText element's default style.
    /// </summary>
    public TextStyle MergeWith(TextStyle? baseStyle)
    {
        if (baseStyle == null) return this;

        return new TextStyle
        {
            FontFamily = FontFamily ?? baseStyle.FontFamily,
            FontWeight = FontWeight != FontWeight.Normal || FontFamily != null ? FontWeight : baseStyle.FontWeight,
            Italic = Italic || (FontFamily == null && baseStyle.Italic),
            FontSize = FontSize ?? baseStyle.FontSize,
            Color = Color ?? baseStyle.Color,
            BackgroundColor = BackgroundColor ?? baseStyle.BackgroundColor,
            Decoration = Decoration != TextDecoration.None ? Decoration : baseStyle.Decoration,
            DecorationColor = DecorationColor ?? baseStyle.DecorationColor,
            DecorationThickness = DecorationThickness ?? baseStyle.DecorationThickness,
            BaselineAlignment = BaselineAlignment != BaselineAlignment.Baseline
                ? BaselineAlignment
                : baseStyle.BaselineAlignment,
            BaselineShift = BaselineShift ?? baseStyle.BaselineShift,
            LetterSpacing = LetterSpacing ?? baseStyle.LetterSpacing,
            FontFeatures = FontFeatures ?? baseStyle.FontFeatures,
            WordSpacing = WordSpacing ?? baseStyle.WordSpacing,
            DecorationStyle = DecorationStyle != DecorationStyle.Solid ? DecorationStyle : baseStyle.DecorationStyle,
            LineHeight = LineHeight ?? baseStyle.LineHeight,
        };
    }

    /// <summary>
    /// Returns the effective font size, defaulting to 12 if not set.
    /// </summary>
    public float EffectiveFontSize => FontSize ?? 12f;

    /// <summary>
    /// Returns the effective text color, defaulting to black if not set.
    /// </summary>
    public Color EffectiveColor => Color ?? BonniePDF.Color.Black;

    /// <summary>
    /// Returns the effective font name, resolving family+weight+italic or defaulting to Helvetica.
    /// When FontFamily is null, uses Helvetica as the base family so that Weight/Italic are still applied
    /// (e.g. Bold with no family → "Helvetica-Bold", not plain "Helvetica").
    /// </summary>
    public string EffectiveFontName =>
        FontFamilyResolver.Resolve(FontFamily ?? "Helvetica", FontWeight, Italic);

    /// <summary>
    /// Returns an independent copy of this style.
    /// </summary>
    /// <remarks>
    /// The fluent builders (<see cref="Bold"/>, <see cref="WithColor"/>,
    /// <c>Subscript()</c>, …) mutate and return <c>this</c>, so a style shared
    /// across several <see cref="RichText"/> spans and mutated between them would
    /// otherwise leak its final state back onto the earlier spans.
    /// <see cref="RichText.Span(string, TextStyle?)"/> clones the style on the way
    /// in for exactly this reason. All fields are value types (<see cref="Color"/>
    /// is immutable), so a shallow copy is a full copy.
    /// </remarks>
    public TextStyle Clone() => (TextStyle)MemberwiseClone();

    // --- Fluent API ---

    /// <summary>Sets font weight to Bold (700)</summary>
    public TextStyle Bold()
    {
        FontWeight = FontWeight.Bold;
        return this;
    }

    /// <summary>Sets font weight to Light (300)</summary>
    public TextStyle Light()
    {
        FontWeight = FontWeight.Light;
        return this;
    }

    /// <summary>Sets font weight to Thin (100)</summary>
    public TextStyle Thin()
    {
        FontWeight = FontWeight.Thin;
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
        FontWeight = weight;
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

    /// <summary>Adds overline decoration (a line above the text)</summary>
    public TextStyle Overline()
    {
        Decoration |= TextDecoration.Overline;
        return this;
    }

    /// <summary>Sets the decoration line colour (underline/strikethrough/overline)</summary>
    public TextStyle WithDecorationColor(Color color)
    {
        DecorationColor = color;
        return this;
    }

    /// <summary>Sets the decoration line thickness in points</summary>
    public TextStyle WithDecorationThickness(float thickness)
    {
        DecorationThickness = thickness;
        return this;
    }

    /// <summary>Raises the text above the baseline (superscript)</summary>
    public TextStyle Superscript()
    {
        BaselineAlignment = BaselineAlignment.Superscript;
        return this;
    }

    /// <summary>Lowers the text below the baseline (subscript)</summary>
    public TextStyle Subscript()
    {
        BaselineAlignment = BaselineAlignment.Subscript;
        return this;
    }

    /// <summary>Sets an explicit baseline shift in points (positive = up)</summary>
    public TextStyle WithBaselineShift(float points)
    {
        BaselineShift = points;
        return this;
    }

    /// <summary>Sets letter spacing (tracking) in points, added after every glyph</summary>
    public TextStyle WithLetterSpacing(float points)
    {
        LetterSpacing = points;
        return this;
    }

    /// <summary>Sets word spacing in points, added after every space</summary>
    public TextStyle WithWordSpacing(float points)
    {
        WordSpacing = points;
        return this;
    }

    /// <summary>Sets the decoration line style (solid/wavy/dashed/dotted)</summary>
    public TextStyle WithDecorationStyle(DecorationStyle style)
    {
        DecorationStyle = style;
        return this;
    }

    /// <summary>Adds a wavy underline (spell-check style)</summary>
    public TextStyle WavyUnderline()
    {
        Decoration |= TextDecoration.Underline;
        DecorationStyle = DecorationStyle.Wavy;
        return this;
    }

    /// <summary>Sets the line-height multiplier</summary>
    public TextStyle WithLineHeight(float multiplier)
    {
        LineHeight = multiplier;
        return this;
    }
}
