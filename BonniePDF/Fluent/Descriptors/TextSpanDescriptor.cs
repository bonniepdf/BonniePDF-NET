
namespace BonniePDF.Fluent;

/// <summary>
/// Provides fluent styling methods for a text span within a <see cref="TextDescriptor"/>.
/// </summary>
public sealed class TextSpanDescriptor
{
    private readonly TextStyle _style;

    internal TextSpanDescriptor(TextStyle style)
    {
        _style = style;
    }

    /// <summary>Sets the font weight to bold.</summary>
    public TextSpanDescriptor Bold()
    {
        _style.Bold();
        return this;
    }

    /// <summary>Sets italic rendering.</summary>
    public TextSpanDescriptor Italic()
    {
        _style.WithItalic();
        return this;
    }

    /// <summary>Sets the font size in points.</summary>
    public TextSpanDescriptor FontSize(float size)
    {
        _style.WithSize(size);
        return this;
    }

    /// <summary>Sets the text color.</summary>
    public TextSpanDescriptor FontColor(Color color)
    {
        _style.WithColor(color);
        return this;
    }

    /// <summary>Sets the font family.</summary>
    public TextSpanDescriptor FontFamily(string family)
    {
        _style.WithFamily(family);
        return this;
    }

    /// <summary>Adds underline decoration.</summary>
    public TextSpanDescriptor Underline()
    {
        _style.Underline();
        return this;
    }

    /// <summary>Adds strikethrough decoration.</summary>
    public TextSpanDescriptor Strikethrough()
    {
        _style.Strikethrough();
        return this;
    }

    /// <summary>Adds overline decoration (a line above the text).</summary>
    public TextSpanDescriptor Overline()
    {
        _style.Overline();
        return this;
    }

    /// <summary>Sets the decoration line colour (underline/strikethrough/overline).</summary>
    public TextSpanDescriptor DecorationColor(Color color)
    {
        _style.WithDecorationColor(color);
        return this;
    }

    /// <summary>Sets the decoration line thickness in points.</summary>
    public TextSpanDescriptor DecorationThickness(float thickness)
    {
        _style.WithDecorationThickness(thickness);
        return this;
    }

    /// <summary>Raises the span above the baseline (superscript).</summary>
    public TextSpanDescriptor Superscript()
    {
        _style.Superscript();
        return this;
    }

    /// <summary>Lowers the span below the baseline (subscript).</summary>
    public TextSpanDescriptor Subscript()
    {
        _style.Subscript();
        return this;
    }

    /// <summary>Sets an explicit baseline shift in points (positive = up).</summary>
    public TextSpanDescriptor BaselineShift(float points)
    {
        _style.WithBaselineShift(points);
        return this;
    }

    /// <summary>Sets letter spacing (tracking) in points, added after every glyph.</summary>
    public TextSpanDescriptor LetterSpacing(float points)
    {
        _style.WithLetterSpacing(points);
        return this;
    }

    /// <summary>Sets word spacing in points, added after every space.</summary>
    public TextSpanDescriptor WordSpacing(float points)
    {
        _style.WithWordSpacing(points);
        return this;
    }

    /// <summary>Sets the decoration line style (solid/wavy/dashed/dotted).</summary>
    public TextSpanDescriptor DecorationLineStyle(DecorationStyle style)
    {
        _style.WithDecorationStyle(style);
        return this;
    }

    /// <summary>Adds a wavy underline (spell-check style).</summary>
    public TextSpanDescriptor WavyUnderline()
    {
        _style.WavyUnderline();
        return this;
    }

    /// <summary>Sets the background highlight color.</summary>
    public TextSpanDescriptor BackgroundColor(Color color)
    {
        _style.WithBackground(color);
        return this;
    }

    /// <summary>Sets the font weight.</summary>
    public TextSpanDescriptor Weight(FontWeight weight)
    {
        _style.WithWeight(weight);
        return this;
    }
}
