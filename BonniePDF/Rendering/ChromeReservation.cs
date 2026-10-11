using System;
using System.Runtime.InteropServices;

namespace BonniePDF;

/// <summary>
/// The vertical space a page's header and footer take out of the body content
/// band, in points. Both are measured against the body content width for the
/// page they belong to; zero means "no template, or the template returned
/// nothing for this page".
/// </summary>
/// <remarks>
/// See the shared specification, "Layout &amp; Pagination" §5.1 (chrome
/// reservation), for the normative rule these numbers feed.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct ChromeReservation(float Header, float Footer)
{
    /// <summary>The reservation for a page with no header and no footer.</summary>
    public static readonly ChromeReservation None = new(0f, 0f);
}

/// <summary>
/// Builds the per-page header/footer measurement the render context uses to
/// inset the body content band.
/// </summary>
internal static class ChromeReservationFactory
{
    /// <summary>
    /// Returns a lazy per-page measurement, or <c>null</c> when the document
    /// declares neither template - which lets the context skip reservation
    /// entirely rather than measure two nothings per page.
    /// </summary>
    /// <remarks>
    /// The delegate reads <see cref="RenderContext.PageSize"/> and
    /// <see cref="RenderContext.Margin"/> at the moment it is asked, so a
    /// document whose pages differ in size or margin measures each page's
    /// chrome against that page's own content width.
    /// </remarks>
    public static Func<int, ChromeReservation>? For(DocumentSettings settings, RenderContext ctx)
    {
        Func<int, IElement?>? header = settings.HeaderTemplate;
        Func<int, IElement?>? footer = settings.FooterTemplate;
        if (header == null && footer == null) return null;

        return pageNumber =>
        {
            float contentWidth = ctx.PageSize.Width - ctx.Margin * 2f;
            return new ChromeReservation(
                Measure(header?.Invoke(pageNumber), contentWidth, settings.ContentDirection),
                Measure(footer?.Invoke(pageNumber), contentWidth, settings.ContentDirection));
        };
    }

    private static float Measure(IElement? element, float contentWidth, ContentDirection direction)
    {
        if (element == null) return 0f;
        ContentDirectionResolver.Apply(element, direction);
        float height = element.MeasureHeight(contentWidth);
        return float.IsFinite(height) && height > 0f ? height : 0f;
    }
}
