using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Fluent.Descriptors;

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
