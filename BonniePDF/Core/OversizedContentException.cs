namespace BonniePDF;

/// <summary>
/// Raised when content that cannot split across pages is taller than a page and the document's
/// <see cref="DocumentSettings.OversizedContent"/> is <see cref="OversizedContent.Throw"/>. With the default,
/// <see cref="OversizedContent.Clip"/>, the content is drawn at the top of a page, cut off at the bottom of the body,
/// and an error diagnostic is recorded instead.
/// </summary>
public sealed class OversizedContentException : BonniePdfException
{
    /// <summary>Creates the exception with the message the diagnostic would have carried.</summary>
    public OversizedContentException(string message) : base(message)
    {
    }
}
