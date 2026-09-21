using System;
using BonniePDF.Elements.Layout.Pagination;

namespace BonniePDF.Fluent.Extensions;

/// <summary>
/// Fluent forms of the pagination control primitives (spec
/// "Layout &amp; Pagination" §7).
/// </summary>
/// <remarks>
/// Each method wraps the rest of the chain, so the primitive governs whatever is
/// written after it:
/// <code>
/// column.Item().EnsureSpace(150).Text("Section 3");
/// column.Item().PreventPageBreak().Text("This paragraph stays together.");
/// column.Item().ShowOnce().Text("Cover note");
/// </code>
/// The traditional API expresses the same primitives as elements
/// (<c>new EnsureSpace(150, heading)</c>); both produce identical output, as the
/// two public APIs must.
/// </remarks>
public static class ContainerPaginationExtensions
{
    /// <summary>
    /// Starts a new page unless at least <paramref name="minHeight"/> points of
    /// body height remain. The orphaned-heading control.
    /// </summary>
    public static IContentContainer EnsureSpace(this IContentContainer c, float minHeight) =>
        c.Element(new EnsureSpace(minHeight));

    /// <summary>
    /// Keeps the wrapped content together, moving it whole to the next page
    /// rather than splitting it across the boundary.
    /// </summary>
    public static IContentContainer PreventPageBreak(this IContentContainer c) =>
        c.Element(new PreventPageBreak());

    /// <summary>
    /// Renders the wrapped content only where it fits whole, never splitting it.
    /// Content taller than a page is cut off rather than continued.
    /// </summary>
    public static IContentContainer ShowEntire(this IContentContainer c) =>
        c.Element(new ShowEntire());

    /// <summary>
    /// Renders what fits on the current page and discards the rest, instead of
    /// continuing onto further pages.
    /// </summary>
    public static IContentContainer StopPaging(this IContentContainer c) =>
        c.Element(new StopPaging());

    /// <summary>
    /// Renders the wrapped content only on the pages <paramref name="predicate"/>
    /// accepts.
    /// </summary>
    public static IContentContainer ShowIf(this IContentContainer c, Func<PageContext, bool> predicate) =>
        c.Element(new ShowIf(predicate));

    /// <summary>Renders the wrapped content on the document's first page only.</summary>
    public static IContentContainer ShowOnce(this IContentContainer c) =>
        c.Element(new ShowOnce());

    /// <summary>Renders the wrapped content on every page except the first.</summary>
    public static IContentContainer SkipOnce(this IContentContainer c) =>
        c.Element(new SkipOnce());

    /// <summary>
    /// Renders the wrapped content outside the flow: it draws from the current
    /// position but consumes no height, so following content overlaps it.
    /// </summary>
    public static IContentContainer Unconstrained(this IContentContainer c) =>
        c.Element(new Unconstrained());
}
