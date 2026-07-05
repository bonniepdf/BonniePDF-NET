namespace BonniePDF.Configuration;

/// <summary>
/// Numbering style for a page label section, mapping to the PDF /S entry.
/// </summary>
public enum PageLabelStyle
{
    /// <summary>Decimal Arabic numerals (1, 2, 3, …). PDF /D.</summary>
    Decimal,
    /// <summary>Lowercase Roman numerals (i, ii, iii, …). PDF /r.</summary>
    LowercaseRoman,
    /// <summary>Uppercase Roman numerals (I, II, III, …). PDF /R.</summary>
    UppercaseRoman,
    /// <summary>Lowercase letters (a, b, c, …, aa, bb, …). PDF /a.</summary>
    LowercaseAlpha,
    /// <summary>Uppercase letters (A, B, C, …, AA, BB, …). PDF /A.</summary>
    UppercaseAlpha,
}

/// <summary>
/// Defines how the PDF viewer labels pages in its page counter, "go to page"
/// dialog, and bookmark targets. Written as <c>/PageLabels</c> in the document
/// catalog (PDF §12.4.2).
/// <para>
/// Setting <see cref="DocumentSettings.PageLabel"/> applies the labelling from the
/// first page of the document. For multi-range labelling (e.g. front matter in
/// Roman numerals followed by decimal body pages) use
/// <see cref="DocumentSettings.PageLabels"/> instead.
/// </para>
/// <example>
/// <code>
/// Settings = new DocumentSettings
/// {
///     PageLabel = new PageLabel { StartAt = 42 }
/// }
/// // → viewer shows "42" on page 1, "43" on page 2, etc.
/// </code>
/// </example>
/// </summary>
public class PageLabel
{
    /// <summary>Numbering style. Defaults to <see cref="PageLabelStyle.Decimal"/>.</summary>
    public PageLabelStyle Style { get; set; } = PageLabelStyle.Decimal;

    /// <summary>First value in the sequence. Defaults to <c>1</c>.</summary>
    public int StartAt { get; set; } = 1;

    /// <summary>
    /// Optional label prefix prepended to every page label
    /// (e.g. <c>"A-"</c> → A-1, A-2, …).
    /// </summary>
    public string? Prefix { get; set; }
}
