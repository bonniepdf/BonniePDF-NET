namespace BonniePDF;

/// <summary>What happens to content that cannot split across pages and is taller than a page.</summary>
public enum OversizedContent
{
    /// <summary>Draw it at the top of a page, cut off at the bottom of the body, and record an error diagnostic (the default).</summary>
    Clip,
    /// <summary>Stop generation with an <see cref="OversizedContentException"/>.</summary>
    Throw
}
