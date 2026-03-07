using BonniePDF.Elements.Text;

namespace BonniePDF.Fluent.Descriptors;

/// <summary>
/// Describes rich text content with multiple spans.
/// Provides methods for adding styled text spans, page numbers, and total page counts.
/// Maps to a <see cref="RichText"/> element internally.
/// </summary>
public sealed class TextDescriptor
{
    internal RichText RichText { get; } = new();

    /// <summary>
    /// Adds a text span with optional styling.
    /// </summary>
    /// <returns>A <see cref="TextSpanDescriptor"/> for configuring the span's style.</returns>
    public TextSpanDescriptor Span(string text)
    {
        var style = new TextStyle();
        RichText.Span(text, style);
        return new TextSpanDescriptor(style);
    }

    /// <summary>
    /// Adds a text span followed by a line break.
    /// </summary>
    /// <returns>A <see cref="TextSpanDescriptor"/> for configuring the span's style.</returns>
    public TextSpanDescriptor Line(string text)
    {
        var style = new TextStyle();
        RichText.Line(text, style);
        return new TextSpanDescriptor(style);
    }

    /// <summary>
    /// Adds a line break.
    /// </summary>
    public void LineBreak()
    {
        RichText.LineBreak();
    }

    /// <summary>
    /// Inserts the current page number.
    /// </summary>
    public TextSpanDescriptor CurrentPageNumber()
    {
        var style = new TextStyle();
        RichText.Span(TextElementBase.TotalPagesToken.Replace("Total", ""), style);
        return new TextSpanDescriptor(style);
    }

    /// <summary>
    /// Inserts the total page count.
    /// </summary>
    public TextSpanDescriptor TotalPages()
    {
        var style = new TextStyle();
        RichText.Span(TextElementBase.TotalPagesToken, style);
        return new TextSpanDescriptor(style);
    }
}
