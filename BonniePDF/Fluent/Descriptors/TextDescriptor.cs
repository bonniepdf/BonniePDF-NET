using System.Collections.Generic;

namespace BonniePDF.Fluent;

/// <summary>
/// Describes rich text content with multiple spans.
/// Provides methods for adding styled text spans, page numbers, and total page counts.
/// Maps to a <see cref="RichText"/> element internally.
/// </summary>
public sealed class TextDescriptor
{
    private enum PartKind
    {
        Span,
        Line,
        Break,
    }

    // The spans are held here, with the live style object each returned TextSpanDescriptor mutates, and
    // turned into a RichText only when the handler has finished (see RichText). RichText snapshots a
    // span's style the moment the span is added, so adding it up front would freeze it before
    // `.Bold()`, `.FontColor()` and the rest had run.
    private readonly List<(PartKind Kind, string Text, TextStyle? Style)> _parts = [];

    private TextAlignment _alignment = TextAlignment.Left;
    private bool _alignmentExplicit;

    /// <summary>The rich text this descriptor describes, with every span's final style.</summary>
    internal RichText RichText
    {
        get
        {
            // Only pass Alignment along when Align()/AlignCenter()/etc. was actually called: assigning it
            // unconditionally would mark the built RichText's Alignment as explicit even when the author
            // never touched it, silently defeating the ambient text-alignment fallback from an enclosing
            // Container (spec 01 "Document Model" §3.2).
            var richText = new RichText();
            if (_alignmentExplicit) richText.Alignment = _alignment;

            // RichText carries its own default style and does not look at the page's, so hand it the
            // page default (DefaultTextStyle) here, the way a TextBlock picks it up when it is created.
            // Spans still override it field by field.
            TextStyle? pageDefault = TextElementBase.AmbientDefaultStyle.Value;
            if (pageDefault != null)
                richText.DefaultStyle = pageDefault.MergeWith(richText.DefaultStyle);

            foreach ((PartKind kind, string text, TextStyle? style) in _parts)
            {
                switch (kind)
                {
                    case PartKind.Span:
                        richText.Span(text, style);
                        break;
                    case PartKind.Line:
                        richText.Line(text, style);
                        break;
                    default:
                        richText.LineBreak();
                        break;
                }
            }

            return richText;
        }
    }

    /// <summary>
    /// Sets the paragraph alignment for the whole text block
    /// (Left, Center, Right, or Justify).
    /// </summary>
    /// <returns>This descriptor for continued chaining.</returns>
    public TextDescriptor Align(TextAlignment alignment)
    {
        _alignment = alignment;
        _alignmentExplicit = true;
        return this;
    }

    /// <summary>Aligns the paragraph to the left (default).</summary>
    public TextDescriptor AlignLeft() => Align(TextAlignment.Left);

    /// <summary>Centers the paragraph.</summary>
    public TextDescriptor AlignCenter() => Align(TextAlignment.Center);

    /// <summary>Aligns the paragraph to the right.</summary>
    public TextDescriptor AlignRight() => Align(TextAlignment.Right);

    /// <summary>Justifies the paragraph (flush on both edges, except the last line).</summary>
    public TextDescriptor Justify() => Align(TextAlignment.Justify);

    /// <summary>
    /// Adds a text span with optional styling.
    /// </summary>
    /// <returns>A <see cref="TextSpanDescriptor"/> for configuring the span's style.</returns>
    public TextSpanDescriptor Span(string text)
    {
        var style = new TextStyle();
        _parts.Add((PartKind.Span, text, style));
        return new TextSpanDescriptor(style);
    }

    /// <summary>
    /// Adds a text span with a predefined style.
    /// </summary>
    public void Span(string text, TextStyle style)
    {
        _parts.Add((PartKind.Span, text, style));
    }

    /// <summary>
    /// Adds a text span followed by a line break.
    /// </summary>
    /// <returns>A <see cref="TextSpanDescriptor"/> for configuring the span's style.</returns>
    public TextSpanDescriptor Line(string text)
    {
        var style = new TextStyle();
        _parts.Add((PartKind.Line, text, style));
        return new TextSpanDescriptor(style);
    }

    /// <summary>
    /// Adds a line break.
    /// </summary>
    public void LineBreak()
    {
        _parts.Add((PartKind.Break, string.Empty, null));
    }

    /// <summary>
    /// Inserts the current page number.
    /// </summary>
    public TextSpanDescriptor CurrentPageNumber()
    {
        var style = new TextStyle();
        _parts.Add((PartKind.Span, TextElementBase.CurrentPageToken, style));
        return new TextSpanDescriptor(style);
    }

    /// <summary>
    /// Inserts the total page count.
    /// </summary>
    public TextSpanDescriptor TotalPages()
    {
        var style = new TextStyle();
        _parts.Add((PartKind.Span, TextElementBase.TotalPagesToken, style));
        return new TextSpanDescriptor(style);
    }
}
