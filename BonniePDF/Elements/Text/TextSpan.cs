namespace BonniePDF;

/// <summary>
/// A styled segment of text within a RichText element.
/// Each span carries its own TextStyle that is merged with the parent's default style at render time.
/// </summary>
public class TextSpan
{
    /// <summary>
    /// The text content of this span.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The style for this span. Properties set to null inherit from the parent RichText's DefaultStyle.
    /// </summary>
    public TextStyle? Style { get; set; }

    /// <summary>
    /// When true, a line break is inserted after this span's text.
    /// </summary>
    public bool IsLineBreak { get; set; }

    /// <summary>
    /// A URI that makes this span a link: each line the span wraps onto becomes a clickable
    /// region, tagged as a Link inside the paragraph, with the span's text as its description.
    /// The link's look is the span's style (colour, underline); nothing is added.
    /// </summary>
    public string? Link { get; set; }
}
