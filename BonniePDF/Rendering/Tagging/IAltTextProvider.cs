namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// Optional companion to <see cref="IRoleProvider"/> for elements that
/// can supply alt-text. When tagging is enabled and the element
/// implements this interface, the pipeline writes <see cref="AltText"/>
/// onto the open structure element's <c>/Alt</c> entry.
///
/// Used by <c>Image</c>, <c>QRCode</c>, and other media elements where
/// PDF/UA requires an alternate text description for assistive tech.
/// </summary>
public interface IAltTextProvider
{
    /// <summary>
    /// The alternate text for this element. <c>null</c> or empty means
    /// "no alt-text"; the renderer treats such elements as
    /// <c>/Artifact</c> rather than <c>/Figure</c> when tagging is on,
    /// and logs a build-time warning so authors don't silently produce
    /// inaccessible figures.
    /// </summary>
    string AltText { get; }

    /// <summary>
    /// When <c>true</c>, the author has explicitly opted into a
    /// decorative-only rendering: the element is tagged as
    /// <c>/Artifact</c> with no warning, even though no alt-text is
    /// provided. Distinguishes "missing alt by mistake" from "no alt
    /// is correct".
    /// </summary>
    bool IsDecorative { get; }
}
